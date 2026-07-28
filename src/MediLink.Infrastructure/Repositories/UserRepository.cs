using Microsoft.EntityFrameworkCore;
using MediLink.Core.Entities;
using MediLink.Core.Interfaces;
using MediLink.Infrastructure.Data;

namespace MediLink.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly MediLinkDbContext _db;

    public UserRepository(MediLinkDbContext db)
    {
        _db = db;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _db.Users
            .Include(u => u.CustomerProfile)
            .Include(u => u.StoreOwnerProfile)
            .ThenInclude(s => s.Stores)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _db.Users.FindAsync(id);
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

    public async Task<User> CreateStoreOwnerAsync(User user, StoreOwnerProfile profile, Store store)
    {
        _db.Users.Add(user);
        profile.UserId = user.Id;
        _db.StoreOwnerProfiles.Add(profile);
        
        store.StoreOwnerProfileId = profile.Id;
        _db.Stores.Add(store);

        await _db.SaveChangesAsync();
        return user;
    }
}
