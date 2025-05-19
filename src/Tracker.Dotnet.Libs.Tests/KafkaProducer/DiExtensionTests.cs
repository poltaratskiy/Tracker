using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;
using Tracker.Dotnet.Libs.KafkaProducer;

namespace Tracker.Dotnet.Libs.Tests.KafkaProducer;

[TestFixture]
public class DiExtensionTests
{
    [Test]
    public void AddKafkaProducer_ShouldThrow_IfBootstrapServersNotSet()
    {
        var services = new ServiceCollection();

        Should.Throw<ArgumentNullException>(() =>
        {
            services.AddKafkaProducer(cfg => {
                cfg.ForMessage<TestMessage>().Topic("some-topic");
                // BootstrapServers wasn't pointed out
            });
        });
    }

    [Test]
    public void AddKafkaProducer_ShouldRegister_KafkaProducer()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Mock.Of<IProducerWrapper>());
        services.AddHttpContextAccessor();

        services.AddKafkaProducer(cfg =>
        {
            cfg.ForMessage<TestMessage>().Topic("topic");
            cfg.BootstrapServers("localhost:9092");
        });

        var provider = services.BuildServiceProvider();
        var producer = provider.GetService<IKafkaProducer>();

        producer.ShouldNotBeNull();
    }

    [Test]
    public void KafkaProducerBuilder_ShouldMap_Type_To_Topic()
    {
        var options = new KafkaProducerOptions();
        var builder = new KafkaProducerBuilder(options);

        builder.ForMessage<TestMessage>().Topic("topic1");

        options.MessageTopicMap.ShouldContainKey(typeof(TestMessage));
        options.MessageTopicMap[typeof(TestMessage)].ShouldBe("topic1");
    }
}
