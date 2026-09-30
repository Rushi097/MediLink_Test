using Microsoft.EntityFrameworkCore;
using MediLink.Core.Entities;
using MediLink.Core.Interfaces;
using MediLink.Infrastructure.Data;

namespace MediLink.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AuthDbContext _db;

    public UserRepository(AuthDbContext db)
    {
        _db = db;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _db.Users
            .Include(u => u.CustomerProfile)
            .Include(u => u.StoreOwnerProfile)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _db.Users
            .Include(u => u.CustomerProfile)
            .Include(u => u.StoreOwnerProfile)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<bool> UserExistsAsync(string email)
    {
        return await _db.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower());
    }

    public async Task<User> CreateCustomerAsync(User user, CustomerProfile profile)
    {
        _db.Users.Add(user);
        profile.UserId = user.Id;
        _db.CustomerProfiles.Add(profile);
        await _db.SaveChangesAsync();
        return user;
    }

    public async Task<int> CountByRoleAsync(MediLink.Core.Enums.UserRole role) => await _db.Users.CountAsync(u => u.Role == role);

    public async Task<User> CreateStoreOwnerAsync(User user, StoreOwnerProfile profile)
    {
        _db.Users.Add(user);
        profile.UserId = user.Id;
        user.StoreOwnerProfile = profile;
        _db.StoreOwnerProfiles.Add(profile);
        await _db.SaveChangesAsync();
        return user;
    }

    public async Task DeleteUserAsync(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return;
        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
    }
}
