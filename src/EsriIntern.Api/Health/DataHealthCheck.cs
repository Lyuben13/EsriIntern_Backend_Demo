using EsriIntern.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EsriIntern.Api.Health;

/// <summary>
/// Health check за проверка на връзката с базата данни
/// </summary>
public class DataHealthCheck : IHealthCheck
{
    private readonly AppDbContext _db;
    private readonly ILogger<DataHealthCheck> _logger;

    public DataHealthCheck(AppDbContext db, ILogger<DataHealthCheck> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Проверяваме дали можем да изпълним проста заявка
            var canConnect = await _db.Database.CanConnectAsync(cancellationToken);
            
            if (!canConnect)
            {
                return HealthCheckResult.Unhealthy("Database connection failed");
            }

            // Проверяваме дали има данни (опционално)
            var hasData = await _db.StatePopulations.AnyAsync(cancellationToken);

            return hasData
                ? HealthCheckResult.Healthy("Database is connected and contains data")
                : HealthCheckResult.Degraded("Database is connected but contains no data yet");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed for database");
            return HealthCheckResult.Unhealthy("Database health check failed", ex);
        }
    }
}
