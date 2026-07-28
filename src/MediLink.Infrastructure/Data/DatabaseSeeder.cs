using MediLink.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediLink.Infrastructure.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(MediLinkDbContext db)
    {
        if (await db.Medicines.AnyAsync()) return;
        db.Medicines.AddRange(
            new Medicine { Name = "Dolo 650 Tablet", Category = "Fever & pain", Description = "Paracetamol tablets for fever and mild pain relief.", Price = 32, StockQuantity = 100 },
            new Medicine { Name = "Cetirizine 10mg", Category = "Allergy care", Description = "Antihistamine tablets for common allergy symptoms.", Price = 24, StockQuantity = 100 },
            new Medicine { Name = "Vitamin C 500mg", Category = "Vitamins", Description = "Vitamin C supplement for daily wellness.", Price = 145, StockQuantity = 75 },
            new Medicine { Name = "ORS Electrolyte", Category = "Wellness", Description = "Oral rehydration solution with electrolytes.", Price = 38, StockQuantity = 100 });
        await db.SaveChangesAsync();
    }
}
