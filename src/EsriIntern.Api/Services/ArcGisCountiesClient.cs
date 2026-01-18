using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace EsriIntern.Api.Services;

public class ArcGisCountiesClient
{
    private readonly HttpClient _http;
    private readonly ArcGisOptions _opt;
    private readonly JsonSerializerOptions _json;

    public ArcGisCountiesClient(HttpClient http, IOptions<ArcGisOptions> options)
    {
        _http = http;
        _opt = options.Value;
        _json = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        // Small default timeout; you can increase.
        _http.Timeout = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Downloads counties and aggregates population per state.
    /// Uses ArcGIS query pagination with resultOffset/resultRecordCount.
    /// </summary>
    public async Task<Dictionary<string, long>> GetPopulationByStateAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_opt.LayerUrl))
            throw new InvalidOperationException("ArcGis:LayerUrl is missing.");

        var queryUrl = _opt.LayerUrl.TrimEnd('/') + "/query";
        var outFields = string.IsNullOrWhiteSpace(_opt.OutFields) ? "STATE_NAME,POPULATION" : _opt.OutFields;
        var pageSize = _opt.MaxRecordCount <= 0 ? 2000 : _opt.MaxRecordCount;

        var totals = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

        int offset = 0;
        while (true)
        {
            var url = BuildQueryUrl(
                queryUrl,
                where: "1=1",
                outFields: outFields,
                offset: offset,
                recordCount: pageSize);

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

            if (resp.StatusCode == HttpStatusCode.Forbidden)
                throw new InvalidOperationException("ArcGIS service returned 403 (maybe requires token). Choose a public layer or add auth.");

            resp.EnsureSuccessStatusCode();

            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            var data = await JsonSerializer.DeserializeAsync<ArcGisQueryResponse>(stream, _json, ct)
                       ?? new ArcGisQueryResponse();

            if (data.Features.Count == 0)
                break;

            foreach (var f in data.Features)
            {
                // Expected fields: STATE_NAME and POPULATION
                var state = GetString(f.Attributes, "STATE_NAME")?.Trim();
                var pop = GetLong(f.Attributes, "POPULATION");

                if (string.IsNullOrWhiteSpace(state))
                    continue;

                totals.TryGetValue(state, out var current);
                totals[state] = current + pop;
            }

            offset += data.Features.Count;

            // Stop when we got less than one page and transfer limit not exceeded
            var exceeded = data.ExceededTransferLimit == true;
            if (!exceeded && data.Features.Count < pageSize)
                break;
        }

        return totals;
    }

    private static string BuildQueryUrl(string baseQueryUrl, string where, string outFields, int offset, int recordCount)
    {
        // ArcGIS query params (docs): where, outFields, returnGeometry, f=json,
        // plus pagination resultOffset/resultRecordCount.
        // We also add "returnDistinctValues=false" implicitly.

        var q = new Dictionary<string, string?>
        {
            ["where"] = where,
            ["outFields"] = outFields,
            ["returnGeometry"] = "false",
            ["f"] = "json",
            ["resultOffset"] = offset.ToString(CultureInfo.InvariantCulture),
            ["resultRecordCount"] = recordCount.ToString(CultureInfo.InvariantCulture)
        };

        var query = string.Join("&", q
            .Where(kv => kv.Value is not null)
            .Select(kv => $"{WebUtility.UrlEncode(kv.Key)}={WebUtility.UrlEncode(kv.Value)}"));

        return baseQueryUrl + "?" + query;
    }

    private static string? GetString(Dictionary<string, object?> attrs, string key)
    {
        if (!attrs.TryGetValue(key, out var v) || v is null)
            return null;

        return v switch
        {
            string s => s,
            JsonElement je when je.ValueKind == JsonValueKind.String => je.GetString(),
            JsonElement je => je.ToString(),
            _ => v.ToString()
        };
    }

    private static long GetLong(Dictionary<string, object?> attrs, string key)
    {
        if (!attrs.TryGetValue(key, out var v) || v is null)
            return 0;

        try
        {
            return v switch
            {
                long l => l,
                int i => i,
                double d => (long)Math.Round(d),
                decimal m => (long)Math.Round(m),
                JsonElement je when je.ValueKind == JsonValueKind.Number && je.TryGetInt64(out var l) => l,
                JsonElement je when je.ValueKind == JsonValueKind.String && long.TryParse(je.GetString(), out var l) => l,
                _ when long.TryParse(v.ToString(), out var l) => l,
                _ => 0
            };
        }
        catch
        {
            return 0;
        }
    }
}
