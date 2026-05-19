using LoanApplication.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using OutboxPublisherService.Infrastructure.BackgroundService;
using Shouldly;

namespace LoanApplicationTests;

public class OutboxProcessorTests
{
    private readonly EligibilityTestFixture _fixture = new();

    [Fact]
    public async Task MarksMessageAsPublished_WhenProcessed()
    {
        var currentTime = new DateTimeOffset(2026, 4, 5, 13, 30, 30, TimeSpan.Zero);
        var fakeTimeProvider = new FakeTimeProvider();
        fakeTimeProvider.AdjustTime(currentTime);

        var sut = await CreateSut(fakeTimeProvider);

        var messageId = Guid.NewGuid();
        await SeedOutboxMessage(messageId, occurredAt: new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc));

        await sut.ProcessAsync(CancellationToken.None);

        await using var db = _fixture.DbFactory.CreateDbContext();
        var message = db.OutboxMessages.Single(m => m.Id == messageId);
        message.PublishedAt.ShouldBe(currentTime.UtcDateTime);
    }

    [Fact]
    public async Task SkipsAlreadyPublishedMessages()
    {
        var sut = await CreateSut(new FakeTimeProvider());

        var messageId = Guid.NewGuid();
        var originalPublishedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        await SeedOutboxMessage(messageId, occurredAt: originalPublishedAt.AddMinutes(-1), publishedAt: originalPublishedAt);

        await sut.ProcessAsync(CancellationToken.None);

        await using var db = _fixture.DbFactory.CreateDbContext();
        db.OutboxMessages.Single(m => m.Id == messageId).PublishedAt.ShouldBe(originalPublishedAt);
    }

    [Fact]
    public async Task ProcessesMultipleUnpublishedMessages()
    {
        var currentTime = new DateTimeOffset(2026, 4, 5, 13, 30, 30, TimeSpan.Zero);
        var fakeTimeProvider = new FakeTimeProvider();
        fakeTimeProvider.AdjustTime(currentTime);

        var sut = await CreateSut(fakeTimeProvider);

        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        await SeedOutboxMessage(firstId, occurredAt: new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc));
        await SeedOutboxMessage(secondId, occurredAt: new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc));

        await sut.ProcessAsync(CancellationToken.None);

        await using var db = _fixture.DbFactory.CreateDbContext();
        db.OutboxMessages.Where(m => m.Id == firstId || m.Id == secondId)
            .ToList()
            .ShouldAllBe(m => m.PublishedAt == currentTime.UtcDateTime);
    }

    private async Task SeedOutboxMessage(
        Guid id,
        DateTime occurredAt,
        string type = "LoanApproved",
        string payload = "{}",
        DateTime? publishedAt = null)
    {
        await using var db = _fixture.DbFactory.CreateDbContext();
        db.OutboxMessages.Add(new OutboxMessage(id, type, payload, occurredAt, publishedAt));
        await db.SaveChangesAsync();
    }

    private async Task<OutboxProcessor> CreateSut(TimeProvider timeProvider)
    {
        var processorFactory = new OutboxProcessorFactory(_fixture.DbFactory, NullLoggerFactory.Instance, timeProvider);
        return await processorFactory.CreateAsync();
    }
}
