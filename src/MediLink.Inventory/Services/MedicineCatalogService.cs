using System.Collections.Concurrent;
using System.Text.Json;
using System.Xml.Linq;

namespace MediLink.Inventory.Services;

public interface IMedicineCatalogService
{
    Task<IReadOnlyList<MedicineCatalogItemDto>> SearchAsync(string name, CancellationToken cancellationToken = default);
    Task<MedicineCatalogItemDto?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MedicineCatalogItemDto>> ListLocalAsync(string? search, int limit, CancellationToken cancellationToken = default);
    Task<MedicineCatalogItemDto> UpsertReferenceAsync(MedicineCatalogItemDto item, CancellationToken cancellationToken = default);
}

public sealed class MedicineCatalogService : IMedicineCatalogService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly FlatFileInventoryStore _files;
    private readonly ConcurrentDictionary<string, (DateTimeOffset Expires, MedicineCatalogItemDto Item)> _cache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);
    private readonly string _placeholderBaseUrl;

    public MedicineCatalogService(IHttpClientFactory httpClientFactory, FlatFileInventoryStore files, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _files = files;
        _placeholderBaseUrl = configuration["MedicineCatalog:PlaceholderBaseUrl"] ?? "http://localhost:5201/api/medicines/catalog/placeholder";
    }

    public async Task<IReadOnlyList<MedicineCatalogItemDto>> ListLocalAsync(string? search, int limit, CancellationToken cancellationToken = default)
    {
        var rows = await _files.GetCatalogAsync(search, cancellationToken);
        return rows.Take(Math.Clamp(limit, 1, 100)).Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<MedicineCatalogItemDto>> SearchAsync(string name, CancellationToken cancellationToken = default)
    {
        var query = name.Trim();
        if (query.Length < 2) return Array.Empty<MedicineCatalogItemDto>();

        var local = await _files.GetCatalogAsync(query, cancellationToken);
        if (local.Count > 0) return local.Take(12).Select(ToDto).ToList();

        var client = _httpClientFactory.CreateClient("medicineCatalog");
        using var response = await client.GetAsync($"drugs.json?name={Uri.EscapeDataString(query)}&expand=psn", cancellationToken);
        if (!response.IsSuccessStatusCode) return Array.Empty<MedicineCatalogItemDto>();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var concepts = new List<(string Id, string Name, string Tty)>();
        if (document.RootElement.TryGetProperty("drugGroup", out var group) && group.TryGetProperty("conceptGroup", out var groups))
        {
            foreach (var conceptGroup in groups.EnumerateArray())
            {
                var tty = conceptGroup.TryGetProperty("tty", out var ttyElement) ? ttyElement.GetString() ?? "" : "";
                if (!conceptGroup.TryGetProperty("conceptProperties", out var properties)) continue;
                foreach (var concept in properties.EnumerateArray())
                {
                    var id = concept.TryGetProperty("rxcui", out var idElement) ? idElement.GetString() : null;
                    var conceptName = concept.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : null;
                    if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(conceptName)) concepts.Add((id!, conceptName!, tty));
                }
            }
        }

        var selected = concepts.Where(x => x.Tty is "SCD" or "SBD" or "IN" or "MIN").GroupBy(x => x.Id).Select(x => x.First()).Take(8).ToList();
        var results = new List<MedicineCatalogItemDto>();
        foreach (var concept in selected)
        {
            var item = await GetByExternalIdAsync(concept.Id, cancellationToken);
            if (item is not null) results.Add(item);
            else results.Add(new MedicineCatalogItemDto
            {
                ExternalId = concept.Id, ExternalSource = "RxNorm", Name = concept.Name, DisplayName = concept.Name,
                GenericName = concept.Name, Category = "Medicine",
                Description = "Standardized medicine reference from the U.S. National Library of Medicine RxNorm catalogue.",
                ImageUrl = Placeholder(concept.Name)
            });
        }
        return results;
    }

    public async Task<MedicineCatalogItemDto?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(externalId)) return null;
        if (_cache.TryGetValue(externalId, out var cached) && cached.Expires > DateTimeOffset.UtcNow) return cached.Item;

        var local = await _files.GetMedicineAsync(externalId, cancellationToken);
        if (local is not null)
        {
            var item = ToDto(local);
            _cache[externalId] = (DateTimeOffset.UtcNow.Add(CacheDuration), item);
            return item;
        }

        var client = _httpClientFactory.CreateClient("medicineCatalog");
        using var rxResponse = await client.GetAsync($"rxcui/{Uri.EscapeDataString(externalId)}.json", cancellationToken);
        if (!rxResponse.IsSuccessStatusCode) return null;
        using var rxDoc = JsonDocument.Parse(await rxResponse.Content.ReadAsStringAsync(cancellationToken));
        var name = rxDoc.RootElement.GetProperty("idGroup").TryGetProperty("name", out var nameElement) ? nameElement.GetString() : null;
        if (string.IsNullOrWhiteSpace(name)) return null;

        var result = new MedicineCatalogItemDto
        {
            ExternalId = externalId, ExternalSource = "RxNorm", Name = name!, DisplayName = name!, GenericName = name!,
            Category = "Medicine", Description = "Standardized medicine reference from the U.S. National Library of Medicine RxNorm catalogue.",
            ImageUrl = Placeholder(name!)
        };

        try
        {
            var dailyMedUrl = $"https://dailymed.nlm.nih.gov/dailymed/services/v1/drugname/{Uri.EscapeDataString(name!)}/human/spls.json";
            using var labelResponse = await client.GetAsync(dailyMedUrl, cancellationToken);
            if (labelResponse.IsSuccessStatusCode)
            {
                using var labelDoc = JsonDocument.Parse(await labelResponse.Content.ReadAsStringAsync(cancellationToken));
                if (labelDoc.RootElement.TryGetProperty("DATA", out var rows) && rows.GetArrayLength() > 0)
                {
                    var setId = rows[0][0].GetString();
                    var title = rows[0].GetArrayLength() > 1 ? rows[0][1].GetString() : null;
                    if (!string.IsNullOrWhiteSpace(title)) result.DisplayName = CleanTitle(title!);
                    if (!string.IsNullOrWhiteSpace(setId))
                    {
                        result.ImageUrl = await GetFirstImageAsync(client, setId!, cancellationToken) ?? result.ImageUrl;
                        var indication = await GetIndicationAsync(client, setId!, cancellationToken);
                        if (!string.IsNullOrWhiteSpace(indication)) result.Description = indication!;
                    }
                }
            }
        }
        catch { }

        _cache[externalId] = (DateTimeOffset.UtcNow.Add(CacheDuration), result);
        return result;
    }

    public async Task<MedicineCatalogItemDto> UpsertReferenceAsync(MedicineCatalogItemDto item, CancellationToken cancellationToken = default)
    {
        var rows = await _files.GetCatalogAsync(null, cancellationToken);
        var existing = rows.FirstOrDefault(x => string.Equals(x.Id, item.ExternalId, StringComparison.OrdinalIgnoreCase));
        // UpsertReference is intentionally implemented through a small direct JSON operation in the file store.
        var record = new MedicineRecord
        {
            InternalId = existing?.InternalId ?? Guid.NewGuid(), Id = item.ExternalId, ExternalSource = item.ExternalSource,
            Name = item.DisplayName, Category = item.Category, Description = item.Description, ImageUrl = item.ImageUrl ?? Placeholder(item.DisplayName), IsActive = true
        };
        await _files.UpsertMedicineAsync(record, cancellationToken);
        return ToDto(record);
    }

    private MedicineCatalogItemDto ToDto(MedicineRecord row) => new()
    {
        ExternalId = row.Id, ExternalSource = row.ExternalSource, Name = row.Name, DisplayName = row.Name,
        GenericName = row.Name, Category = row.Category, Description = row.Description, ImageUrl = ToAbsoluteImageUrl(row.ImageUrl, row.Name)
    };

    private string Placeholder(string name) => $"{_placeholderBaseUrl}?name={Uri.EscapeDataString(name)}";
    private string ToAbsoluteImageUrl(string? imageUrl, string name)
    {
        if (!string.IsNullOrWhiteSpace(imageUrl) && Uri.TryCreate(imageUrl, UriKind.Absolute, out _)) return imageUrl;
        return Placeholder(name);
    }

    private static async Task<string?> GetFirstImageAsync(HttpClient client, string setId, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await client.GetAsync($"https://dailymed.nlm.nih.gov/dailymed/services/v2/spls/{setId}/media.json", cancellationToken);
            if (!response.IsSuccessStatusCode) return null;
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            if (!document.RootElement.TryGetProperty("data", out var data) || !data.TryGetProperty("media", out var media)) return null;
            foreach (var file in media.EnumerateArray())
            {
                var mime = file.TryGetProperty("mime_type", out var mimeElement) ? mimeElement.GetString() : null;
                if (mime?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true && file.TryGetProperty("url", out var url)) return url.GetString();
            }
        }
        catch { }
        return null;
    }

    private static async Task<string?> GetIndicationAsync(HttpClient client, string setId, CancellationToken cancellationToken)
    {
        try
        {
            var xml = await client.GetStringAsync($"https://dailymed.nlm.nih.gov/dailymed/services/v2/spls/{setId}.xml", cancellationToken);
            var document = XDocument.Parse(xml);
            var section = document.Descendants().FirstOrDefault(x => x.Name.LocalName == "section" && x.Descendants().Any(c => c.Name.LocalName == "code" && (c.Attribute("displayName")?.Value ?? "").Contains("INDICATIONS AND USAGE", StringComparison.OrdinalIgnoreCase)));
            if (section is null) return null;
            var text = string.Join(" ", section.DescendantNodes().OfType<XText>().Select(x => x.Value.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)));
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();
            return text.Length > 1200 ? text[..1200] + "…" : text;
        }
        catch { return null; }
    }
    private static string CleanTitle(string title)
    {
        var index = title.IndexOf(" [", StringComparison.Ordinal);
        return index > 0 ? title[..index].Trim() : title.Trim();
    }
}

public sealed class MedicineCatalogItemDto
{
    public string ExternalId { get; set; } = string.Empty;
    public string ExternalSource { get; set; } = "RxNorm";
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string GenericName { get; set; } = string.Empty;
    public string Category { get; set; } = "Medicine";
    public string Description { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}
