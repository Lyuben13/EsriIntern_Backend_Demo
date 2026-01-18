using System.ComponentModel.DataAnnotations;

namespace EsriIntern.Api.Services;

/// <summary>
/// Конфигурация за background worker
/// </summary>
public class WorkerOptions
{
    /// <summary>
    /// Интервал за периодично изпълнение в минути
    /// </summary>
    [Range(1, 10080, ErrorMessage = "IntervalMinutes must be between 1 and 10080 (1 week)")]
    public int IntervalMinutes { get; set; } = 120;
}
