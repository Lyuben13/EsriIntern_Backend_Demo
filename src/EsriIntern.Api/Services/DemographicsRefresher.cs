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
            if (totals.Count == 0)
                throw new InvalidOperationException("ArcGIS returned no state populations.");

            // Keep the previous snapshot if writing its replacement fails.
            await using var transaction = await _db.Database.BeginTransactionAsync(ct);
            var now = DateTime.UtcNow;
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
            await transaction.CommitAsync(ct);
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
