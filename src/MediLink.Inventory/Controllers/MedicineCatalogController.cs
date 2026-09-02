using MediLink.Inventory.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text;

namespace MediLink.Inventory.Controllers;

[ApiController]
[Route("api/medicines/catalog")]
public class MedicineCatalogController : ControllerBase
{
    private readonly IMedicineCatalogService _catalog;

    public MedicineCatalogController(IMedicineCatalogService catalog)
    {
        _catalog = catalog;
    }

    [HttpGet("list")]
    public async Task<IActionResult> List([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 24, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var all = await _catalog.ListLocalAsync(search, 100, cancellationToken);
        var items = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Ok(new { success = true, items, total = all.Count, page, pageSize });
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length < 2)
            return BadRequest(new { success = false, message = "Enter at least 2 characters to search medicines." });

        var items = await _catalog.SearchAsync(name, cancellationToken);
        return Ok(new { success = true, items });
    }

    [HttpGet("placeholder")]
    public IActionResult Placeholder([FromQuery] string name = "Medicine")
    {
        name ??= "Medicine";
        var safe = WebUtility.HtmlEncode(name.Length > 48 ? name[..48] : name);
        var svg = $"""<svg xmlns="http://www.w3.org/2000/svg" width="800" height="520" viewBox="0 0 800 520"><defs><linearGradient id="bg" x1="0" x2="1"><stop offset="0" stop-color="#eef8f7"/><stop offset="1" stop-color="#f8fbff"/></linearGradient></defs><rect width="800" height="520" rx="32" fill="url(#bg)"/><rect x="170" y="80" width="460" height="270" rx="26" fill="white" stroke="#d7e8e8" stroke-width="3"/><rect x="230" y="150" width="340" height="110" rx="55" fill="#eaf3ff" stroke="#2f6fad" stroke-width="5"/><path d="M400 150v110" stroke="#2f6fad" stroke-width="5"/><rect x="210" y="155" width="90" height="100" rx="18" fill="#fff"/><text x="400" y="410" text-anchor="middle" font-family="Arial,sans-serif" font-size="30" font-weight="700" fill="#123d49">{safe}</text><text x="400" y="450" text-anchor="middle" font-family="Arial,sans-serif" font-size="17" fill="#617d88">MediLink catalogue reference</text></svg>""";
        return File(Encoding.UTF8.GetBytes(svg), "image/svg+xml");
    }

    [HttpGet("{externalId}")]
    public async Task<IActionResult> Get(string externalId, CancellationToken cancellationToken)
    {
        var item = await _catalog.GetByExternalIdAsync(externalId, cancellationToken);
        return item is null
            ? NotFound(new { success = false, message = "Medicine was not found in the external catalogue." })
            : Ok(new { success = true, item });
    }
}
