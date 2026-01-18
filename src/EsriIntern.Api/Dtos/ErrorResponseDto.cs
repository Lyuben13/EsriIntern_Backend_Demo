namespace EsriIntern.Api.Dtos
{
    /// <summary>
    /// Стандартизиран DTO за error responses
    /// </summary>
    public class ErrorResponseDto
    {
        public int Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
        public DateTime Timestamp { get; set; }

        public ErrorResponseDto()
        {
            Timestamp = DateTime.UtcNow;
        }
    }
}
