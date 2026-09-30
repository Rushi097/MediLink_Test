using MediLink.Core.Enums;
using MediLink.Core.Entities;

namespace MediLink.Core.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(Guid id);
    Task<User> CreateCustomerAsync(User user, CustomerProfile profile);
    Task<User> CreateStoreOwnerAsync(User user, StoreOwnerProfile profile);
    Task DeleteUserAsync(Guid userId);
    Task<int> CountByRoleAsync(UserRole role);
    Task<bool> UserExistsAsync(string email);
}