using EsriIntern.Api.Services;
using Microsoft.Extensions.Options;

namespace EsriIntern.Api.Background;

public class DemographicsWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly WorkerOptions _opt;
    private readonly ILogger<DemographicsWorker> _log;

    public DemographicsWorker(IServiceScopeFactory scopeFactory, IOptions<WorkerOptions> options, ILogger<DemographicsWorker> log)
    {
        _scopeFactory = scopeFactory;
        _opt = options.Value;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Initial run
        await SafeRunOnce(stoppingToken);

        var minutes = _opt.IntervalMinutes <= 0 ? 120 : _opt.IntervalMinutes;
        _log.LogInformation("DemographicsWorker started. Interval: {Minutes} minutes", minutes);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(minutes));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await SafeRunOnce(stoppingToken);
        }
    }

    private async Task SafeRunOnce(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var refresher = scope.ServiceProvider.GetRequiredService<DemographicsRefresher>();
            await refresher.RefreshAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // normal stop
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Demographics refresh failed");
        }
    }
}
