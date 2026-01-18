namespace EsriIntern.Api.Dtos;

/// <summary>
/// Response DTO за демографски данни на щат
/// </summary>
public class StatePopulationResponseDto
{
    /// <summary>
    /// Име на щат
    /// </summary>
    public string StateName { get; set; } = string.Empty;

    /// <summary>
    /// Общо население на щата
    /// </summary>
    public long Population { get; set; }

    /// <summary>
    /// UTC дата и час на последното извличане на данните
    /// </summary>
    public DateTime RetrievedAtUtc { get; set; }
}
