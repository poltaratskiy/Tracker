using Confluent.Kafka;
using Confluent.Kafka.Admin;
using System.Diagnostics;
using Testcontainers.PostgreSql;
using Tracker.Dotnet.Libs.LoadTests.Configuration;

namespace Tracker.Dotnet.Libs.LoadTests.Infrastructure;

public class TestEnvironment : IAsyncDisposable
{
    private static string ComposeFile = Path.Combine("Infrastructure", "kafka.yaml");

    public PostgreSqlContainer Postgres { get; } = new PostgreSqlBuilder("postgres:16.13-alpine3.23")
        .WithDatabase(ConfigConstants.PostgresDbName)
        .WithPortBinding(ConfigConstants.PostgresPort, 5432)
        .WithUsername(ConfigConstants.PostgresUserName)
        .WithPassword(ConfigConstants.PostgresPassword)
        .Build();

    public async Task StartAsync()
    {
        await Postgres.StartAsync();
        await WaitForKafkaAsync(TimeSpan.FromSeconds(50));
        await CreateTopic();
    }

    public async Task RecreateTopicAsync()
    {
        await DeleteTopic();
        await CreateTopic();
    }

    private async Task CreateTopic()
    {
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
                ReplicationFactor = 3
            }
        });

        await admin.CreateTopicsAsync(new[]
        {
            new TopicSpecification
            {
                Name = ConfigConstants.KafkaLatencyTestTopic,
                NumPartitions = 6,
                ReplicationFactor = 3
            }
        });
    }

    private async Task DeleteTopic()
    {
        var adminConfig = new AdminClientConfig
        {
            BootstrapServers = $"{ConfigConstants.KafkaBootstrapServer}"
        };

        using var admin = new AdminClientBuilder(adminConfig).Build();

        await admin.DeleteTopicsAsync(new[] { ConfigConstants.KafkaTopic, ConfigConstants.KafkaLatencyTestTopic });
    }

    private async Task WaitForKafkaAsync(TimeSpan timeout)
    {
        await Run("docker", $"compose -f {ComposeFile} up -d");
        var started = DateTime.UtcNow;

        while (DateTime.UtcNow - started < timeout)
        {
            try
            {
                var adminConfig = new AdminClientConfig
                {
                    BootstrapServers = $"{ConfigConstants.KafkaBootstrapServer}"
                };

                using var admin = new AdminClientBuilder(adminConfig).Build();

                var metadata = admin.GetMetadata(TimeSpan.FromSeconds(3));

                if (metadata.Brokers.Count >= 3)
                    return;
            }
            catch (Exception)
            {
                await Task.Delay(1000);
            }
        }

        throw new TimeoutException();
    }

    private static async Task Run(string file, string args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = file,
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = Process.Start(psi)!;

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
            throw new Exception("Docker command failed");
    }

    public async ValueTask DisposeAsync()
    {
        await Postgres.DisposeAsync();
        await Run("docker", $"compose -f {ComposeFile} down -v");
    }
}
