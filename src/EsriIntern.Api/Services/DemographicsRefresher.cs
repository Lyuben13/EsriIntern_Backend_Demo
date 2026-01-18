using EsriIntern.Api.Data;
using EsriIntern.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EsriIntern.Api.Services;

public class DemographicsRefresher
{
    private readonly ArcGisCountiesClient _client;
    private readonly AppDbContext _db;
    private readonly ILogger<DemographicsRefresher> _log;

    public DemographicsRefresher(ArcGisCountiesClient client, AppDbContext db, ILogger<DemographicsRefresher> log)
    {
        _client = client;
        _db = db;
        _log = log;
    }

    public async Task<DateTime> RefreshAsync(CancellationToken ct)
    {
        _log.LogInformation("Starting demographics refresh...");
        
        try
        {
            var totals = await _client.GetPopulationByStateAsync(ct);
            var now = DateTime.UtcNow;

        // Keep only latest snapshot per state:
        // - remove old (batch delete - една SQL заявка)
        // - insert new
        // For a real system you might keep history in a separate table.

        // Оптимизирано: използваме ExecuteDeleteAsync за batch delete вместо индивидуални DELETE заявки
        await _db.StatePopulations.ExecuteDeleteAsync(ct);

        foreach (var kv in totals.OrderBy(k => k.Key))
        {
            _db.StatePopulations.Add(new StatePopulationSnapshot
            {
                StateName = kv.Key,
                Population = kv.Value,
                RetrievedAtUtc = now
            });
        }

            await _db.SaveChangesAsync(ct);
            _log.LogInformation("Demographics refresh finished. States: {Count}", totals.Count);

            return now;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to refresh demographics data");
            throw;
        }
    }
}
