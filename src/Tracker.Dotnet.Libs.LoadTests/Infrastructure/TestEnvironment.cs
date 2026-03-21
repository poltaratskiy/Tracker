using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Testcontainers.PostgreSql;
using Testcontainers.Redpanda;
using Tracker.Dotnet.Libs.LoadTests.Configuration;

namespace Tracker.Dotnet.Libs.LoadTests.Infrastructure;

public class TestEnvironment : IAsyncDisposable
{
    public RedpandaContainer Kafka { get; } = new RedpandaBuilder("docker.redpanda.com/redpandadata/redpanda:latest")
        .WithCommand("redpanda start --overprovisioned --smp 1 --memory 256M --reserve-memory 0M --node-id 0 --check=false")
        .WithPortBinding(9092, false)
        .Build();

    public PostgreSqlContainer Postgres { get; } = new PostgreSqlBuilder("postgres:16.13-alpine3.23")
        .WithDatabase(ConfigConstants.PostgresDbName)
        .WithPortBinding(ConfigConstants.PostgresPort, 5432)
        .WithUsername(ConfigConstants.PostgresUserName)
        .WithPassword(ConfigConstants.PostgresPassword)
        .Build();

    public async Task StartAsync()
    {
        await Postgres.StartAsync();
        await Kafka.StartAsync();

        var adminConfig = new AdminClientConfig
        {
            BootstrapServers = $"{ConfigConstants.KafkaBootstrapServer}"
        };

        using var admin = new AdminClientBuilder(adminConfig).Build();

        await admin.CreateTopicsAsync(new[]
        {
            new TopicSpecification
            {
                Name = ConfigConstants.KafkaTopic,
                NumPartitions = 6,
                ReplicationFactor = 1
            }
        });
    }

    public async ValueTask DisposeAsync()
    {
        await Kafka.DisposeAsync();
        await Postgres.DisposeAsync();
    }
}
