using Microsoft.Extensions.Options;
using SWP_BE.Options;

namespace SWP_BE.Services;

public sealed class MarketPulseScheduler : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<MarketPulseOptions> _options;
    private readonly ILogger<MarketPulseScheduler> _logger;

    public MarketPulseScheduler(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<MarketPulseOptions> options,
        ILogger<MarketPulseScheduler> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var current = _options.CurrentValue;
            if (!current.Enabled)
            {
                _logger.LogInformation("MarketPulse scheduler is disabled. Sleeping 1 hour.");
                await SafeDelay(TimeSpan.FromHours(1), stoppingToken);
                continue;
            }

            var delay = ComputeDelayToNextRun(current);
            _logger.LogInformation("Next MarketPulse run in {Delay} (at {Time} {Zone}).",
                delay, DateTimeOffset.UtcNow + delay, current.TimeZoneId);
            if (!await SafeDelay(delay, stoppingToken))
            {
                break;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var runner = scope.ServiceProvider.GetRequiredService<IMarketPulseRunner>();
                await runner.RunAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "MarketPulse scheduled run failed.");
            }

            await SafeDelay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private static TimeSpan ComputeDelayToNextRun(MarketPulseOptions options)
    {
        TimeZoneInfo timeZone;
        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(options.TimeZoneId);
        }
        catch
        {
            timeZone = TimeZoneInfo.Utc;
        }

        var nowLocal = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timeZone);
        var todayRun = new DateTimeOffset(
            nowLocal.Year, nowLocal.Month, nowLocal.Day,
            options.ScheduleHour, options.ScheduleMinute, 0,
            nowLocal.Offset);

        var nextRunLocal = nowLocal < todayRun ? todayRun : todayRun.AddDays(1);
        var delay = nextRunLocal - nowLocal;
        return delay < TimeSpan.FromSeconds(5) ? TimeSpan.FromSeconds(5) : delay;
    }

    private static async Task<bool> SafeDelay(TimeSpan delay, CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(delay, stoppingToken);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
