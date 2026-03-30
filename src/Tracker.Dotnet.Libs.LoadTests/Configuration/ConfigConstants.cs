namespace Tracker.Dotnet.Libs.LoadTests.Configuration;

// Configuration is here
internal static class ConfigConstants
{
    internal static string PostgresHost = "localhost";
    internal static int PostgresPort = 15432;
    internal static string PostgresDbName = "Tracker-test";
    internal static string PostgresUserName = "postgres";
    internal static string PostgresPassword = "postgres";

    // Do not change schema and table name without recreating migrations
    internal static string PostgresInboxSchema = "public";
    internal static string PostgresInboxTable = "Inbox";

    internal static string GetConnectionString() =>
        $"Host={PostgresHost};Port={PostgresPort};Database={PostgresDbName};Username={PostgresUserName};Password={PostgresPassword}";

    internal static string KafkaBootstrapServer = "localhost:19092,localhost:29092,localhost:39092";
    internal static string KafkaConsumerGroup = "test-group";
    internal static string KafkaTopic = "test-topic";
    internal static string KafkaLatencyTestTopic = "test-latency-topic";
}
