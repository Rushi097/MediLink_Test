using MediLink.Core.Enums;

namespace MediLink.Core.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties for role-specific details
    public CustomerProfile? CustomerProfile { get; set; }
    public StoreOwnerProfile? StoreOwnerProfile { get; set; }
}