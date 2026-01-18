using System.ComponentModel.DataAnnotations;

namespace EsriIntern.Api.Services;

/// <summary>
/// Конфигурация за ArcGIS Feature Service
/// </summary>
public class ArcGisOptions
{
    /// <summary>
    /// URL на ArcGIS Feature Layer (без /query)
    /// </summary>
    [Required(ErrorMessage = "LayerUrl is required")]
    [Url(ErrorMessage = "LayerUrl must be a valid URL")]
    public string LayerUrl { get; set; } = string.Empty;

    /// <summary>
    /// Полета за извличане от Feature Service (comma-separated)
    /// </summary>
    public string OutFields { get; set; } = "STATE_NAME,POPULATION";

    /// <summary>
    /// Максимален брой записи на страница при пагинация
    /// </summary>
    [Range(1, 10000, ErrorMessage = "MaxRecordCount must be between 1 and 10000")]
    public int MaxRecordCount { get; set; } = 2000;
}
