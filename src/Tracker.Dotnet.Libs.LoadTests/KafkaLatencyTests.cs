using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;
using System.Collections;
using Tracker.Dotnet.Libs.KafkaConsumer;
using Tracker.Dotnet.Libs.KafkaProducer;
using Tracker.Dotnet.Libs.LoadTests.Configuration;
using Tracker.Dotnet.Libs.LoadTests.ConsumerLatencyTestHandler;
using Tracker.Dotnet.Libs.LoadTests.Infrastructure;
using Tracker.Dotnet.Libs.LoadTests.Persistence;

namespace Tracker.Dotnet.Libs.LoadTests;

[TestFixture]
[Ignore("Local only")]
public class KafkaLatencyTests
{
    private const int ProducerInstancesNumber = 1;
    private const int ConsumerInstancesNumber = 3;
    private const int MessagesPerInstance = 1000;
    private static TimeSpan Timeout = TimeSpan.FromSeconds(100); // Overall time after expired calls Cancel() and test completes

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

    [TestCaseSource(nameof(TestCases))]
    public async Task LoadTest(KafkaAcks acks, bool idempotency, bool useInbox)
    {
        _logger.Information($"Running load test: acks: {acks.ToString()}, idempotency: {idempotency}, useInbox: {useInbox}");
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

        var pct = new ProcessingCompletitionTracker();
        pct.SetExpectedCount(MessagesPerInstance * ProducerInstancesNumber);

        var consumerInstances = new List<ServiceProvider>();
        for (int i = 0; i < ConsumerInstancesNumber; i++)
        {
            consumerInstances.Add(genericSp.GetConsumerServiceCollection(spConfiguration, pct));
        }

        var consumers = new List<IKafkaGeneralConsumer>();
        foreach (var consumerInstance in consumerInstances)
        {
            consumers.Add(consumerInstance.GetRequiredService<IKafkaGeneralConsumer>());
        }

        var producerInstances = new List<ServiceProvider>();
        var producers = new List<IKafkaProducer>();
        for (int i = 0; i < ProducerInstancesNumber; i++)
        {
            var sp = genericSp.GetProducerServiceCollection(spConfiguration);
            producerInstances.Add(sp);
            producers.Add(sp.GetRequiredService<IKafkaProducer>());
        }

        var consumerLoopTasks = consumers
            .Select(x => x.StartConsumeAsync(pct.CTS.Token))
            .ToArray();

        var producerTasks = producers.Select(ProduceMessagesAsync);

        pct.CTS.CancelAfter(Timeout);
        await Task.WhenAll(producerTasks.ToArray());

        try
        {
            await pct.Completion.WaitAsync(Timeout);
        }
        catch (TimeoutException)
        {
            _logger.Warning("Timeout occured");
        }

        pct.CTS.Cancel();

        try
        {
            await Task.WhenAll(consumerLoopTasks);
        }
        catch (OperationCanceledException)
        {
        }

        var processedMessages = await dbContext.LatencyTestMessages.AsNoTracking().ToArrayAsync();

        var totalTime = processedMessages.Max(x => x.DateReceived) - processedMessages.Min(x => x.DateSent);
        var sortedDurations = processedMessages
                .Select(x => ((TimeSpan)x.Duration!))
                .OrderBy(x => x)
                .ToArray();

        var p50 = Percentile(sortedDurations, 0.5m);
        var p95 = Percentile(sortedDurations, 0.95m);
        var p99 = Percentile(sortedDurations, 0.99m);

        _logger.Information($"Total time: {totalTime}, p50: {p50}, p95: {p95}, p99: {p99}, messages processed: {processedMessages.Length}");

        var groupedByMessageId = processedMessages.GroupBy(x => x.MessageId);
        var filtered = groupedByMessageId.Where(x => x.Count() > 1);
        var duplicatesCount = filtered.Count();

        var lostCount = MessagesPerInstance * ProducerInstancesNumber - processedMessages.Select(x => x.MessageId).ToHashSet().Count;

        _logger.Information($"Total process time for all instances: {totalTime}, total messages: {processedMessages.Length} of {MessagesPerInstance * ProducerInstancesNumber}");
        _logger.Information($"Duplicates: {duplicatesCount}, Messages lost: {lostCount}");

        await dbContext.LatencyTestMessages.ExecuteDeleteAsync();

        await dbInstance.DisposeAsync();
        await Task.WhenAll(producerInstances.Select(x => x.DisposeAsync().AsTask()));
        await Task.WhenAll(consumerInstances.Select(x => x.DisposeAsync().AsTask()));

        Assert.Pass();
    }

    private async Task ProduceMessagesAsync(IKafkaProducer producer, int index)
    {
        var errors = new List<string>();
        int successCount = 0;
        int failedCount = 0;

        for (int i = 0; i < MessagesPerInstance; i++)
        {
            var content = Guid.NewGuid().ToString();

            try
            {
                var message = new LatencyTestMessage(content, DateTime.UtcNow);
                await producer.ProduceAsync(message);
                Interlocked.Increment(ref successCount);
            }
            catch (Exception ex)
            {
                errors.Add(ex.GetType().Name + ": " + ex.Message);
                Interlocked.Increment(ref failedCount);
            }
        }

        _logger.Information($"Producer: #{index}, success num: {successCount}, failed num: {failedCount}");
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
        await _environment.RecreateTopicAsync();
    }

    private static IEnumerable TestCases()
    {
        yield return new TestCaseData(KafkaAcks.None, false, false);
        yield return new TestCaseData(KafkaAcks.All, false, false);
        yield return new TestCaseData(KafkaAcks.All, true, false);

        yield return new TestCaseData(KafkaAcks.None, false, true);
        yield return new TestCaseData(KafkaAcks.All, false, true);
        yield return new TestCaseData(KafkaAcks.All, true, true);
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _environment.DisposeAsync();
        await _logger.DisposeAsync();
    }
}
