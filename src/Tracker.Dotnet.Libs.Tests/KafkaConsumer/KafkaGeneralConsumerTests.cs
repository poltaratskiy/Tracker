using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using System.Text;
using System.Text.Json;
using Tracker.Dotnet.Libs.KafkaAbstractions;
using Tracker.Dotnet.Libs.KafkaConsumer;
using Tracker.Dotnet.Libs.KafkaProducer;

namespace Tracker.Dotnet.Libs.Tests.KafkaConsumer;

[TestFixture]
public class KafkaGeneralConsumerTests
{
    public class TestMessage : IMessage { public string Value { get; set; } = "test"; }

    public class TestHandler : IHandler<TestMessage>
    {
        public bool WasCalled = false;
        public bool ShouldThrow = false;

        public Task HandleAsync(TestMessage message, CancellationToken token)
        {
            WasCalled = true;
            if (ShouldThrow) throw new Exception("Simulated failure");
            return Task.CompletedTask;
        }
    }

    [Test]
    public async Task Should_Process_Message_Via_DI()
    {
        var services = new ServiceCollection();

        services.AddKafkaConsumer(cfg =>
        {
            cfg.BootstrapServers("localhost:9092")
               .ForMessage<TestMessage>()
               .Handler<TestHandler>()
               .Topic("test-topic");
            cfg.ConsumerGroup("test-group")
                .InstantRetries(0);
        });

        var handler = new TestHandler();
        var serialized = JsonSerializer.Serialize(new TestMessage { Value = "FromKafka" });
        var headers = new Headers { { "refid", Encoding.UTF8.GetBytes("abc-123") } };

        var consumeResult = new ConsumeResult<Ignore, string>
        {
            Topic = "test-topic",
            Message = new Message<Ignore, string> { Value = serialized, Headers = headers }
        };

        var cts = new CancellationTokenSource();
        cts.CancelAfter(200);

        var consumerWrapperMock = new Mock<IConsumerWrapper>();
        consumerWrapperMock.Setup(c => c.Consume(It.IsAny<CancellationToken>())).Returns(consumeResult);
        consumerWrapperMock.Setup(c => c.Commit(It.IsAny<ConsumeResult<Ignore, string>>())).Callback(cts.Cancel);

        var producerWrapperMock = new Mock<IProducerWrapper>();
        producerWrapperMock.Setup(c => c.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeliveryResult<string, string>());

        services.AddSingleton<IConsumerWrapper>(consumerWrapperMock.Object);
        services.AddSingleton<IProducerWrapper>(producerWrapperMock.Object);
        services.AddSingleton(handler);
        services.AddLogging();

        var provider = services.BuildServiceProvider();

        var kafka = new KafkaGeneralConsumer(
            provider.GetRequiredService<ILogger<KafkaGeneralConsumer>>(),
            provider.GetRequiredService<IConsumerWrapper>(),
            provider.GetRequiredService<IProducerWrapper>(),
            provider,
            provider.GetRequiredService<KafkaConsumerOptions>()
        );

        await kafka.StartConsumeAsync(cts.Token);

        handler.WasCalled.ShouldBeTrue();
    }

    [Test]
    public async Task Should_Not_Commit_When_Handler_Throws()
    {
        var services = new ServiceCollection();

        

        services.AddKafkaConsumer(cfg =>
        {
            cfg.BootstrapServers("localhost:9092")
               .ForMessage<TestMessage>()
               .Handler<TestHandler>()
               .Topic("test-topic");
            cfg.ConsumerGroup("test-group")
                .InstantRetries(0)
                .SetDeadLetterTopic("DLQ");
        });

        var handler = new TestHandler { ShouldThrow = true };
        var serialized = JsonSerializer.Serialize(new TestMessage { Value = "Boom" });
        var headers = new Headers { { "refid", Encoding.UTF8.GetBytes("abc-123") } };

        var consumeResult = new ConsumeResult<Ignore, string>
        {
            Topic = "test-topic",
            Message = new Message<Ignore, string> { Value = serialized, Headers = headers }
        };

        var cts = new CancellationTokenSource();

        var consumerWrapperMock = new Mock<IConsumerWrapper>();
        consumerWrapperMock.Setup(c => c.Consume(It.IsAny<CancellationToken>())).Returns(consumeResult);
        consumerWrapperMock.Setup(c => c.Commit(It.IsAny<ConsumeResult<Ignore, string>>()));

        var producerWrapperMock = new Mock<IProducerWrapper>();
        producerWrapperMock.Setup(c => c.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeliveryResult<string, string>())
            .Callback(cts.Cancel);

        services.AddSingleton<IProducerWrapper>(producerWrapperMock.Object);

        services.AddSingleton<IConsumerWrapper>(consumerWrapperMock.Object);
        services.AddSingleton(handler);
        services.AddLogging();

        var provider = services.BuildServiceProvider();
        var kafka = provider.GetRequiredService<IKafkaGeneralConsumer>();

        await kafka.StartConsumeAsync(cts.Token);
        await Task.Delay(50);

        handler.WasCalled.ShouldBeTrue();
        consumerWrapperMock.Verify(c => c.Commit(It.IsAny<ConsumeResult<Ignore, string>>()), Times.AtLeastOnce);
        producerWrapperMock.Verify(c => c.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, string>>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
