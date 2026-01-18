namespace EsriIntern.Api.Dtos;

/// <summary>
/// Response DTO за refresh операция
/// </summary>
public class RefreshResponseDto
{
    /// <summary>
    /// UTC дата и час на опресняването
    /// </summary>
    public DateTime RefreshedAtUtc { get; set; }

    /// <summary>
    /// Брой обработени щати
    /// </summary>
    public int? StatesCount { get; set; }
}
