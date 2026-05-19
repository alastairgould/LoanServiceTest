using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EligibilityService;

public class EligibilityWorker(
    EligibilityProcessor processor,
    ILogger<EligibilityWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(60);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        do
        {
            try
            {
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
