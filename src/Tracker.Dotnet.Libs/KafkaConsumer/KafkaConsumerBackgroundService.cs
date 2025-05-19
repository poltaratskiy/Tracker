using Microsoft.Extensions.Hosting;

namespace Tracker.Dotnet.Libs.KafkaConsumer;

public class KafkaConsumerBackgroundService : BackgroundService
{
    public IKafkaGeneralConsumer _kafkaGeneralConsumer;

    public KafkaConsumerBackgroundService(IKafkaGeneralConsumer kafkaGeneralConsumer)
    {
        _kafkaGeneralConsumer = kafkaGeneralConsumer;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return _kafkaGeneralConsumer.StartConsumeAsync(stoppingToken);
    }
}
