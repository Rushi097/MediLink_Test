namespace MediLink.Core.Entities;

public class Store
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    
    public Guid StoreOwnerProfileId { get; set; }
    public StoreOwnerProfile StoreOwnerProfile { get; set; } = null!;
}
