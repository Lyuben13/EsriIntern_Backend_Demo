using System.Text.Json.Serialization;

namespace EsriIntern.Api.Services;

public sealed class ArcGisQueryResponse
{
    [JsonPropertyName("features")]
    public List<ArcGisFeature> Features { get; set; } = new();

    // Some ArcGIS services return this when more records exist.
    [JsonPropertyName("exceededTransferLimit")]
    public bool? ExceededTransferLimit { get; set; }
}

public sealed class ArcGisFeature
{
    [JsonPropertyName("attributes")]
    public Dictionary<string, object?> Attributes { get; set; } = new();
}
