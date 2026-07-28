using Microsoft.AspNetCore.Mvc;

namespace MediLink.Api.Controllers;

/// <summary>
/// Public medicine catalogue. This seed catalogue keeps the React client usable
/// while the database-backed inventory module is developed.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MedicinesController : ControllerBase
{
    private static readonly IReadOnlyList<MedicineResponse> Catalogue =
    [
        new(1, "Dolo 650 Tablet", "Fever & pain", 32, "15 tablets", true),
        new(2, "Cetirizine 10mg", "Allergy care", 24, "10 tablets", true),
        new(3, "Vitamin C 500mg", "Vitamins", 145, "30 chewable tablets", true),
        new(4, "Digene Gel", "Digestive care", 118, "200 ml", true),
        new(5, "ORS Electrolyte", "Wellness", 38, "200 ml", true),
        new(6, "Volini Spray", "Pain relief", 210, "100 g", true)
    ];

    [HttpGet]
    public IActionResult GetAll([FromQuery] string? search, [FromQuery] string? category)
    {
        var medicines = Catalogue.Where(m =>
            (string.IsNullOrWhiteSpace(search) ||
             m.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
             m.Category.Contains(search, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrWhiteSpace(category) || m.Category.Equals(category, StringComparison.OrdinalIgnoreCase)));

        return Ok(new { success = true, items = medicines, total = medicines.Count() });
    }

    [HttpGet("{id:int}")]
    public IActionResult GetById(int id)
    {
        var medicine = Catalogue.FirstOrDefault(m => m.Id == id);
        return medicine is null
            ? NotFound(new { success = false, message = "Medicine was not found." })
            : Ok(new { success = true, item = medicine });
    }

    [HttpGet("categories")]
    public IActionResult GetCategories() => Ok(new { success = true, items = Catalogue.Select(m => m.Category).Distinct() });

    public record MedicineResponse(int Id, string Name, string Category, decimal Price, string PackSize, bool InStock);
}
