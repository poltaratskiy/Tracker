using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;
using Tracker.Dotnet.Libs.KafkaConsumer;
using Tracker.Dotnet.Libs.KafkaProducer;
using Tracker.Dotnet.Libs.LoadTests.Configuration;
using Tracker.Dotnet.Libs.LoadTests.ConsumerTestHandler;
using Tracker.Dotnet.Libs.LoadTests.Infrastructure;
using Tracker.Dotnet.Libs.LoadTests.Persistence;

namespace Tracker.Dotnet.Libs.LoadTests;

[TestFixture]
[Ignore("Local only")]
public class KafkaTests
{
    private const int ProducerInstancesNumber = 3;
    private const int ConsumerInstancesNumber = 3;
    private const int MessagesPerInstance = 20;
    private static TimeSpan Timeout = TimeSpan.FromSeconds(10); // Overall time after expired calls Cancel() and test completes

    private TestEnvironment _environment;
    private Logger _logger;

    [OneTimeSetUp]
    public async Task Setup()
    {
        _logger = new LoggerConfiguration()
           .MinimumLevel.Information()
           .WriteTo.NUnitOutput()
           .CreateLogger();

        _environment = new TestEnvironment();
        await _environment.StartAsync();
    }

    public async Task LoadTest(KafkaAcks acks, bool idempotency, bool useInbox, bool parallelPubSub)
    {
        var genericSp = new GenericServiceProvider();

        var dbInstance = genericSp.GetDbServiceCollection();
        using var scope = dbInstance.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        await dbContext.Database.MigrateAsync();

        var spConfiguration = new SpConfiguration
        {
            KafkaAcks = acks,
            KafkaIdempotency = idempotency,
            UseTransationalInbox = useInbox
        };

        var consumerInstances = new List<ServiceProvider>();
        for (int i = 0;i < ConsumerInstancesNumber; i++)
        {
            consumerInstances.Add(genericSp.GetConsumerServiceCollection(spConfiguration));
        }

        var consumerTasks = new List<Task>();
        var consumers = new List<(IKafkaGeneralConsumer consumer, ProcessingCompletitionTracker pct)>();
        foreach (var consumerInstance in consumerInstances)
        {
            var pct = consumerInstance.GetRequiredService<ProcessingCompletitionTracker>();
            pct.SetExpectedCount(MessagesPerInstance);
            pct.CTS.CancelAfter(Timeout);
            consumerTasks.Add(pct.Completion);

            var consumer = consumerInstance.GetRequiredService<IKafkaGeneralConsumer>();
            consumers.Add((consumer, pct));

            if (parallelPubSub)
            {
                await consumer.StartConsumeAsync(pct.CTS.Token);
            }
        }

        var producerInstances = new List<ServiceProvider>();
        var producers = new List<IKafkaProducer>();
        for (int i = 0; i < ProducerInstancesNumber; i++)
        {
            var sp = genericSp.GetProducerServiceCollection(spConfiguration);
            producerInstances.Add(sp);
            producers.Add(sp.GetRequiredService<IKafkaProducer>());
        }

        var producerTasks = producers.Select(ProduceMessagesAsync);

        if (parallelPubSub)
        {
            var tasks = producerTasks.Union(consumerTasks).ToArray();
            await Task.WhenAll(tasks);
        }
        else
        {
            await Task.WhenAll(producerTasks);

            await Task.WhenAll(consumers.Select(x => x.consumer.StartConsumeAsync(x.pct.CTS.Token)));
            await Task.WhenAll(consumerTasks);
        }

        

        var processedMessages = await dbContext.ProcessedMessages.ToListAsync();

        var consumeStart = processedMessages.Min(x => x.DateStart);
        var consumeEnd = processedMessages.Max(x => x.DateEnd);

        var groupedByInstance = processedMessages
            .GroupBy(x => x.InstanceId)
            .ToArray();

        foreach (var group in groupedByInstance)
        {
            var messages = group.ToArray();

            var totalTime = messages.Max(x => x.DateEnd) - messages.Min(x => x.DateStart);

            var sortedDurations = messages
                .Select(x => ((TimeSpan)x.Duration!))
                .OrderBy(x => x)
                .ToArray();

            var p50 = Percentile(sortedDurations, 0.5m);
            var p95 = Percentile(sortedDurations, 0.95m);
            var p99 = Percentile(sortedDurations, 0.99m);

            _logger.Information($"Consumer: #{group.Key} Total time: {totalTime}, p50: {p50}, p95: {p95}, p99: {p99}");
        }

        _logger.Information($"Total consume time for all instances: {consumeEnd - consumeStart}");

        processedMessages.Clear();
        await dbContext.SaveChangesAsync();
    }

    private async Task ProduceMessagesAsync(IKafkaProducer producer, int index)
    {
        var durations = new List<TimeSpan>(MessagesPerInstance);
        var timeStart = DateTime.Now;

        for (int i = 0; i < MessagesPerInstance; i++)
        {
            var iterationStart = DateTime.Now;
            await producer.ProduceAsync(new KafkaTestMessage(Guid.NewGuid().ToString()));
            var iterationEnd = DateTime.Now;
            durations.Add(iterationEnd - iterationStart);
        }

        var totalTime = DateTime.Now - timeStart;

        var p50 = Percentile(durations, 0.5m);
        var p95 = Percentile(durations, 0.95m);
        var p99 = Percentile(durations, 0.99m);

        _logger.Information($"Producer: #{index} Total time: {totalTime}, p50: {p50}, p95: {p95}, p99: {p99}");
    }

    private static TimeSpan Percentile(IEnumerable<TimeSpan> timespans, decimal percentile)
    {
        if (timespans == null || !timespans.Any())
            throw new ArgumentException("Data is empty");

        if (percentile <= 0 || percentile > 1)
            throw new ArgumentOutOfRangeException(nameof(percentile));

        var sorted = timespans.OrderBy(x => x).ToArray();

        var index = (int)Math.Ceiling(percentile * sorted.Length) - 1;
        index = Math.Clamp(index, 0, sorted.Length - 1);

        return sorted[index];
    }

    [TearDown]
    public async Task TearDown()
    {
        await _environment.Postgres.ExecScriptAsync($"delete from \"{ConfigConstants.PostgresInboxSchema}\".\"{ConfigConstants.PostgresInboxTable}\"");
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _environment.DisposeAsync();
        await _logger.DisposeAsync();
    }
}
