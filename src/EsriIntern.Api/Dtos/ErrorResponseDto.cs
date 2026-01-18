namespace EsriIntern.Api.Dtos;

/// <summary>
/// Стандартизиран DTO за error responses
/// </summary>
public class ErrorResponseDto
{
    /// <summary>
    /// HTTP статус код
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// Съобщение за грешката
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Опционални детайли за грешката (например в Development режим)
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Timestamp на грешката
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
