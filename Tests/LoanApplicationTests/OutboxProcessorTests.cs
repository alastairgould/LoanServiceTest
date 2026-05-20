using LoanApplication.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using OutboxPublisherService.Features.PublishOutbox;
using OutboxPublisherService.Infrastructure.BackgroundService;
using Shouldly;

namespace LoanApplicationTests;

public class OutboxProcessorTests
{
    private static readonly DateTimeOffset CurrentTime = new(2026, 4, 5, 13, 30, 30, TimeSpan.Zero);
    private readonly EligibilityTestFixture _fixture = new();

    [Fact]
    public async Task MarksMessageAsPublished_WhenProcessed()
    {
        var sut = await CreateSut();

        var messageId = Guid.NewGuid();
        await SeedOutboxMessage(messageId, occurredAt: new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc));

        await sut.ProcessAsync(CancellationToken.None);

        await using var db = await _fixture.DbFactory.CreateDbContextAsync();
        var message = db.OutboxMessages.Single(m => m.Id == messageId);
        message.PublishedAt.ShouldBe(CurrentTime.UtcDateTime);
    }

    [Fact]
    public async Task SkipsAlreadyPublishedMessages()
    {
        var sut = await CreateSut();

        var messageId = Guid.NewGuid();
        var originalPublishedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        await SeedOutboxMessage(messageId, occurredAt: originalPublishedAt.AddMinutes(-1), publishedAt: originalPublishedAt);

        await sut.ProcessAsync(CancellationToken.None);

        await using var db = await _fixture.DbFactory.CreateDbContextAsync();
        db.OutboxMessages.Single(m => m.Id == messageId).PublishedAt.ShouldBe(originalPublishedAt);
    }

    [Fact]
    public async Task ProcessesMultipleUnpublishedMessages()
    {
        var sut = await CreateSut();

        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        await SeedOutboxMessage(firstId, occurredAt: new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc));
        await SeedOutboxMessage(secondId, occurredAt: new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc));

        await sut.ProcessAsync(CancellationToken.None);

        await using var db = await _fixture.DbFactory.CreateDbContextAsync();
        db.OutboxMessages.Where(m => m.Id == firstId || m.Id == secondId)
            .ToList()
            .ShouldAllBe(m => m.PublishedAt == CurrentTime.UtcDateTime);
    }

    [Fact]
    public async Task ProcessesUpToBatchSize_WhenMoreUnpublishedMessages()
    {
        var sut = await CreateSut(batchSize: 2);

        await SeedOutboxMessage(Guid.NewGuid(), occurredAt: new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc));
        await SeedOutboxMessage(Guid.NewGuid(), occurredAt: new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc));
        await SeedOutboxMessage(Guid.NewGuid(), occurredAt: new DateTime(2026, 4, 3, 0, 0, 0, DateTimeKind.Utc));

        await sut.ProcessAsync(CancellationToken.None);

        await using var db = await _fixture.DbFactory.CreateDbContextAsync();
        db.OutboxMessages.Count(m => m.PublishedAt != null).ShouldBe(2);
        db.OutboxMessages.Count(m => m.PublishedAt == null).ShouldBe(1);
    }

    [Fact]
    public async Task IsolatesFailure_WhenOneMessageThrows()
    {
        var firstId = Guid.NewGuid();
        var poisonId = Guid.NewGuid();
        var lastId = Guid.NewGuid();

        await SeedOutboxMessage(firstId, occurredAt: new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc));
        await SeedOutboxMessage(poisonId, occurredAt: new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc));
        await SeedOutboxMessage(lastId, occurredAt: new DateTime(2026, 4, 3, 0, 0, 0, DateTimeKind.Utc));

        var context = await _fixture.DbFactory.CreateDbContextAsync();
        var handler = new ThrowingHandler(context, CreateTimeProvider(), poisonId);
        await using var sut = new OutboxProcessor(
            context, handler, NullLogger<OutboxProcessor>.Instance, batchSize: 500);

        await sut.ProcessAsync(CancellationToken.None);

        await using var db = await _fixture.DbFactory.CreateDbContextAsync();
        db.OutboxMessages.Single(m => m.Id == firstId).PublishedAt.ShouldBe(CurrentTime.UtcDateTime);
        db.OutboxMessages.Single(m => m.Id == lastId).PublishedAt.ShouldBe(CurrentTime.UtcDateTime);
        db.OutboxMessages.Single(m => m.Id == poisonId).PublishedAt.ShouldBeNull();
    }

    private async Task SeedOutboxMessage(
        Guid id,
        DateTime occurredAt,
        string type = "LoanApproved",
        string payload = "{}",
        DateTime? publishedAt = null)
    {
        await using var db = await _fixture.DbFactory.CreateDbContextAsync();
        db.OutboxMessages.Add(new OutboxMessage(id, type, payload, occurredAt, publishedAt));
        await db.SaveChangesAsync();
    }

    private async Task<OutboxProcessor> CreateSut(TimeProvider? timeProvider = null, int batchSize = 500)
    {
        timeProvider ??= CreateTimeProvider();
        var processorFactory = new OutboxProcessorFactory(
            _fixture.DbFactory, NullLoggerFactory.Instance, timeProvider, batchSize);
        return await processorFactory.CreateAsync();
    }

    private static FakeTimeProvider CreateTimeProvider(DateTimeOffset? currentTime = null)
    {
        var provider = new FakeTimeProvider();
        provider.AdjustTime(currentTime ?? CurrentTime);
        return provider;
    }

    private sealed class ThrowingHandler(LoanContext context, TimeProvider timeProvider, Guid targetMessageId)
        : OutboxMessageHandler(context, NullLogger<OutboxMessageHandler>.Instance, timeProvider)
    {
        public override Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken)
        {
            if (message.Id == targetMessageId)
                throw new InvalidOperationException("simulated handler failure");
            return base.HandleAsync(message, cancellationToken);
        }
    }
}
