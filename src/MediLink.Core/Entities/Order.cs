namespace MediLink.Core.Entities;

public enum OrderStatus
{
    // 0-4 preserve the values used by the previous v9 database.
    PendingStoreAcceptance = 0,
    Accepted = 1,
    Preparing = 2,
    Delivered = 3,
    Cancelled = 4,
    ReadyForDelivery = 5,
    OutForDelivery = 6,
    Rejected = 7
}

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string DeliveryAddress { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = "CashOnDelivery";
    public Guid ReservationId { get; set; } = Guid.NewGuid();
    public OrderStatus Status { get; set; } = OrderStatus.PendingStoreAcceptance;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public Guid MedicineId { get; set; }
    public Guid StoreId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}
