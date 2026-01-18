namespace EsriIntern.Api.Dtos
{
    /// <summary>
    /// Response DTO за health check endpoint
    /// </summary>
    public class HealthResponseDto
    {
        /// <summary>
        /// Статус на приложението
        /// </summary>
        public string Status { get; set; } = "ok";

        /// <summary>
        /// UTC дата и час
        /// </summary>
        public DateTime Utc { get; set; }

        public HealthResponseDto()
        {
            Utc = DateTime.UtcNow;
        }
    }
}
