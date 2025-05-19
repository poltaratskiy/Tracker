using Confluent.Kafka;
using Microsoft.AspNetCore.Http;
using Moq;
using Shouldly;
using Tracker.Dotnet.Libs.KafkaProducer;

namespace Tracker.Dotnet.Libs.Tests.KafkaProducer;

[TestFixture]
public class KafkaProducerTests
{
    private KafkaProducerOptions _options;
    private Mock<IProducerWrapper> _producerMock;
    private Libs.KafkaProducer.KafkaProducer _kafkaProducer;

    [SetUp]
    public void Setup()
    {
        var contextAccessorMock = new Mock<IHttpContextAccessor>();
        contextAccessorMock.Setup(h => h.HttpContext).Returns(new DefaultHttpContext());
        _options = new KafkaProducerOptions();
        _producerMock = new Mock<IProducerWrapper>();
        _kafkaProducer = new Libs.KafkaProducer.KafkaProducer(_options, _producerMock.Object, contextAccessorMock.Object);
    }

    [Test]
    public async Task ProduceAsync_Should_Send_Message_To_Correct_Topic()
    {
        // Arrange
        var testMessage = new TestMessage { Text = "Hello" };
        _options.MessageTopicMap[typeof(TestMessage)] = "test-topic";

        // Act
        await _kafkaProducer.ProduceAsync(testMessage);

        // Assert
        _producerMock.Verify(p => p.ProduceAsync(
            "test-topic",
            It.Is<Message<string, string>>(m =>
                m.Value.Contains("Hello") &&
                !string.IsNullOrEmpty(m.Key)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ProduceAsync_Should_Serialize_Message_Correct()
    {
        // Arrange
        var testMessage = new TestMessage { Text = "Привет" };
        _options.MessageTopicMap[typeof(TestMessage)] = "test-topic";

        // Act
        await _kafkaProducer.ProduceAsync(testMessage);

        // Assert
        _producerMock.Verify(p => p.ProduceAsync(
            "test-topic",
            It.Is<Message<string, string>>(m =>
                m.Value.Contains("Привет") &&
                !string.IsNullOrEmpty(m.Key)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ProduceAsync_Should_Throw_If_No_Topic_Configured()
    {
        // Arrange
        var testMessage = new TestMessage { Text = "Hello" };

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            await _kafkaProducer.ProduceAsync(testMessage);
        });

        ex.Message.ShouldContain(nameof(TestMessage));
        _producerMock.Verify(p => p.ProduceAsync(
            It.IsAny<string>(),
            It.IsAny<Message<string, string>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}

public class TestMessage
{
    public string Text { get; set; } = string.Empty;
}
