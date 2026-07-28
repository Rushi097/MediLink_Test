namespace MediLink.Core.Entities;

public class StoreOwnerProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    
    public string BusinessLicenseNumber { get; set; } = string.Empty;
    public List<Store> Stores { get; set; } = new();
}
