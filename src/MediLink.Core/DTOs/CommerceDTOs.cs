using System.ComponentModel.DataAnnotations;

namespace MediLink.Core.DTOs;

public class MedicineCreateRequest
{
    [Required, StringLength(120)] public string Name { get; set; } = string.Empty;
    [Required, StringLength(80)] public string Category { get; set; } = string.Empty;
    [StringLength(1000)] public string Description { get; set; } = string.Empty;
    [Range(0.01, 100000)] public decimal Price { get; set; }
    [Range(0, 100000)] public int StockQuantity { get; set; }
    [Url] public string? ImageUrl { get; set; }
}

public class CartItemRequest
{
    public Guid MedicineId { get; set; }
    [Range(1, 50)] public int Quantity { get; set; } = 1;
}

public class CheckoutRequest
{
    [Required, StringLength(300)] public string DeliveryAddress { get; set; } = string.Empty;
}
