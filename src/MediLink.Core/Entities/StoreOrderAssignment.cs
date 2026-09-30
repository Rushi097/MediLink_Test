namespace MediLink.Core.Entities;

/// <summary>Order-service-owned assignment. StoreId is a reference to the Inventory service entity.</summary>
public class StoreOrderAssignment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StoreId { get; set; }
    public Guid OrderId { get; set; }
    // Snapshot the pharmacy identity at checkout so historical orders remain
    // readable even if the store later changes its profile.
    public string StoreName { get; set; } = string.Empty;
    public string StoreAddress { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
