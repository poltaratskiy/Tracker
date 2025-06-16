using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using System.Text;
using System.Text.Json;
using Tracker.Dotnet.Libs.KafkaConsumer;

namespace Tracker.Dotnet.Libs.Tests.KafkaConsumer;

[TestFixture]
public class KafkaGeneralConsumerTests
{
    public class TestMessage { public string Value { get; set; } = "test"; }

    public class TestHandler : IKafkaConsumer<TestMessage>
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
            cfg.ConsumerGroup("test-group");
        });

        var handler = new TestHandler();
        var serialized = JsonSerializer.Serialize(new TestMessage { Value = "FromKafka" });
        var headers = new Headers { { "refid", Encoding.UTF8.GetBytes("abc-123") } };

        var consumeResult = new ConsumeResult<Ignore, string>
        {
            Topic = "test-topic",
            Message = new Message<Ignore, string> { Value = serialized, Headers = headers }
        };

        var consumerWrapperMock = new Mock<IConsumerWrapper>();
        consumerWrapperMock.Setup(c => c.Consume(It.IsAny<CancellationToken>())).Returns(consumeResult);
        consumerWrapperMock.Setup(c => c.Commit(It.IsAny<ConsumeResult<Ignore, string>>()));

        services.AddSingleton<IConsumerWrapper>(consumerWrapperMock.Object);
        services.AddSingleton(handler);
        services.AddLogging();

        var provider = services.BuildServiceProvider();

        var kafka = new KafkaGeneralConsumer(
            provider.GetRequiredService<ILogger<KafkaGeneralConsumer>>(),
            provider.GetRequiredService<IConsumerWrapper>(),
            provider,
            provider.GetRequiredService<KafkaConsumerOptions>()
        );

        var cts = new CancellationTokenSource();
        cts.CancelAfter(200);

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
            cfg.ConsumerGroup("test-group");
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
        cts.CancelAfter(200);

        var consumerWrapperMock = new Mock<IConsumerWrapper>();
        consumerWrapperMock.Setup(c => c.Consume(It.IsAny<CancellationToken>())).Returns(consumeResult);
        consumerWrapperMock.Setup(c => c.Commit(It.IsAny<ConsumeResult<Ignore, string>>()));

        services.AddSingleton<IConsumerWrapper>(consumerWrapperMock.Object);
        services.AddSingleton(handler);
        services.AddLogging();

        var provider = services.BuildServiceProvider();
        var kafka = provider.GetRequiredService<IKafkaGeneralConsumer>();

        await kafka.StartConsumeAsync(cts.Token);
        await Task.Delay(50);

        handler.WasCalled.ShouldBeTrue();
        consumerWrapperMock.Verify(c => c.Commit(It.IsAny<ConsumeResult<Ignore, string>>()), Times.Never);
    }
}
