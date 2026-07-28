using MediLink.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediLink.Infrastructure.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(MediLinkDbContext db)
    {
        var catalogue = new[]
        {
            new Medicine { Name = "Dolo 650 Tablet", Category = "Fever & pain", Description = "Paracetamol tablets for fever and mild pain relief.", Price = 32, StockQuantity = 100 },
            new Medicine { Name = "Crocin Advance 500mg", Category = "Fever & pain", Description = "Paracetamol tablets for everyday fever and pain relief.", Price = 24, StockQuantity = 100 },
            new Medicine { Name = "Combiflam Tablet", Category = "Fever & pain", Description = "Pain-relief tablet. Use only on pharmacist or doctor advice.", Price = 42, StockQuantity = 80 },
            new Medicine { Name = "Cetirizine 10mg", Category = "Allergy care", Description = "Antihistamine tablets for common allergy symptoms.", Price = 24, StockQuantity = 100 },
            new Medicine { Name = "Allegra 120mg", Category = "Allergy care", Description = "Non-drowsy allergy relief tablets.", Price = 165, StockQuantity = 55 },
            new Medicine { Name = "Otrivin Nasal Spray", Category = "Allergy care", Description = "Nasal congestion relief spray.", Price = 92, StockQuantity = 60 },
            new Medicine { Name = "Vitamin C 500mg", Category = "Vitamins", Description = "Vitamin C supplement for daily wellness.", Price = 145, StockQuantity = 75 },
            new Medicine { Name = "Zincovit Tablet", Category = "Vitamins", Description = "Multivitamin and zinc nutritional supplement.", Price = 118, StockQuantity = 70 },
            new Medicine { Name = "Supradyn Daily", Category = "Vitamins", Description = "Daily multivitamin supplement.", Price = 210, StockQuantity = 50 },
            new Medicine { Name = "ORS Electrolyte", Category = "Wellness", Description = "Oral rehydration solution with electrolytes.", Price = 38, StockQuantity = 100 },
            new Medicine { Name = "Electral Powder", Category = "Wellness", Description = "Electrolyte powder for hydration.", Price = 22, StockQuantity = 120 },
            new Medicine { Name = "Digene Gel", Category = "Digestive care", Description = "Antacid gel for acidity and indigestion.", Price = 118, StockQuantity = 65 },
            new Medicine { Name = "ENO Fruit Salt", Category = "Digestive care", Description = "Fast relief from acidity and indigestion.", Price = 18, StockQuantity = 110 },
            new Medicine { Name = "Isabgol Husk", Category = "Digestive care", Description = "Dietary fibre for digestive wellness.", Price = 145, StockQuantity = 45 },
            new Medicine { Name = "Volini Spray", Category = "Pain relief", Description = "Topical spray for muscle and joint discomfort.", Price = 210, StockQuantity = 55 },
            new Medicine { Name = "Moov Cream", Category = "Pain relief", Description = "Topical cream for muscular discomfort.", Price = 135, StockQuantity = 60 },
            new Medicine { Name = "Vicks Vaporub", Category = "Cold & cough", Description = "Topical vapour rub for cold comfort.", Price = 95, StockQuantity = 85 },
            new Medicine { Name = "Strepsils Lozenges", Category = "Cold & cough", Description = "Soothing throat lozenges.", Price = 45, StockQuantity = 95 },
            new Medicine { Name = "Himalaya Neem Face Wash", Category = "Personal care", Description = "Daily cleansing face wash.", Price = 155, StockQuantity = 45 },
            new Medicine { Name = "Savlon Hand Sanitizer", Category = "Personal care", Description = "Alcohol-based hand sanitizer.", Price = 60, StockQuantity = 90 },
            new Medicine { Name = "Dettol Antiseptic Liquid", Category = "First aid", Description = "Antiseptic liquid for first-aid use.", Price = 115, StockQuantity = 70 },
            new Medicine { Name = "Band Aid Washproof", Category = "First aid", Description = "Washproof adhesive bandages.", Price = 42, StockQuantity = 100 },
            new Medicine { Name = "Accu-Chek Test Strips", Category = "Diabetes care", Description = "Blood glucose monitoring test strips.", Price = 799, StockQuantity = 25 },
            new Medicine { Name = "Dr. Morepen Glucometer", Category = "Diabetes care", Description = "Blood glucose monitoring device.", Price = 699, StockQuantity = 30 }
        };
        var knownNames = await db.Medicines.Select(medicine => medicine.Name).ToListAsync();
        db.Medicines.AddRange(catalogue.Where(medicine => !knownNames.Contains(medicine.Name)));
        await db.SaveChangesAsync();
    }
}
