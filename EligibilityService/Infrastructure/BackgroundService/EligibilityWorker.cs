using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EligibilityService.Infrastructure.BackgroundService;

public class EligibilityWorker(
    EligibilityProcessorFactory processorFactory,
    ILogger<EligibilityWorker> logger) : Microsoft.Extensions.Hosting.BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(60);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        do
        {
            try
            {
                await using var processor = await processorFactory.CreateAsync(stoppingToken);
                await processor.ProcessAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Eligibility processing iteration failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
