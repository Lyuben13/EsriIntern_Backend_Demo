namespace EsriIntern.Api.Models;

public class StatePopulationSnapshot
{
    public long Id { get; set; }
    public string StateName { get; set; } = string.Empty;
    public long Population { get; set; }
    public DateTime RetrievedAtUtc { get; set; }
}
