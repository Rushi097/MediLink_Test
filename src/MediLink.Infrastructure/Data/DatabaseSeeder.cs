using MediLink.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediLink.Infrastructure.Data;

/// <summary>
/// Seeds a local, curated medicine reference catalogue for demos/offline development.
/// Store-specific price and stock are intentionally NOT seeded here.
/// The catalogue can still fall back to RxNorm/DailyMed for medicines outside this seed set.
/// </summary>
public static class DatabaseSeeder
{
    private static readonly (string Id, string Name, string Category, string Description)[] Catalog =
    [
        ("ML-1", "Paracetamol 500 mg", "Fever & Pain", "A commonly used analgesic and antipyretic medicine for relief of pain and fever."),
        ("ML-2", "Paracetamol 650 mg", "Fever & Pain", "A commonly used analgesic and antipyretic medicine for relief of pain and fever."),
        ("ML-3", "Ibuprofen 200 mg", "Fever & Pain", "A nonsteroidal anti-inflammatory medicine used for pain, fever and inflammation."),
        ("ML-4", "Ibuprofen 400 mg", "Fever & Pain", "A nonsteroidal anti-inflammatory medicine used for pain, fever and inflammation."),
        ("ML-5", "Aspirin 75 mg", "Cardiovascular", "Low-dose aspirin is used in specific cardiovascular settings under medical advice."),
        ("ML-6", "Cetirizine 10 mg", "Allergy Care", "An antihistamine used to relieve symptoms of allergic conditions such as sneezing and itching."),
        ("ML-7", "Levocetirizine 5 mg", "Allergy Care", "An antihistamine used to relieve symptoms associated with allergic conditions."),
        ("ML-8", "Loratadine 10 mg", "Allergy Care", "A non-sedating antihistamine used for relief of common allergy symptoms."),
        ("ML-9", "Fexofenadine 120 mg", "Allergy Care", "An antihistamine used to relieve symptoms of allergic rhinitis and related conditions."),
        ("ML-10", "Amoxicillin 500 mg", "Antibiotics", "A penicillin-class antibiotic used for certain bacterial infections when prescribed."),
        ("ML-11", "Amoxicillin + Clavulanate", "Antibiotics", "A combination antibiotic used for certain bacterial infections when prescribed."),
        ("ML-12", "Azithromycin 500 mg", "Antibiotics", "A macrolide antibiotic used for selected bacterial infections when prescribed."),
        ("ML-13", "Doxycycline 100 mg", "Antibiotics", "A tetracycline-class antibiotic used for selected bacterial infections and other indications."),
        ("ML-14", "Cefixime 200 mg", "Antibiotics", "A cephalosporin antibiotic used for selected bacterial infections when prescribed."),
        ("ML-15", "Cefuroxime 500 mg", "Antibiotics", "A cephalosporin antibiotic used for selected bacterial infections when prescribed."),
        ("ML-16", "Metformin 500 mg", "Diabetes Care", "An oral medicine commonly used to help control blood glucose in type 2 diabetes."),
        ("ML-17", "Metformin 1000 mg", "Diabetes Care", "An oral medicine commonly used to help control blood glucose in type 2 diabetes."),
        ("ML-18", "Glimepiride 1 mg", "Diabetes Care", "An oral sulfonylurea medicine used to help control blood glucose in type 2 diabetes."),
        ("ML-19", "Glimepiride 2 mg", "Diabetes Care", "An oral sulfonylurea medicine used to help control blood glucose in type 2 diabetes."),
        ("ML-20", "Amlodipine 5 mg", "Blood Pressure", "A calcium-channel blocker commonly used to treat high blood pressure and certain heart conditions."),
        ("ML-21", "Amlodipine 10 mg", "Blood Pressure", "A calcium-channel blocker commonly used to treat high blood pressure and certain heart conditions."),
        ("ML-22", "Losartan 50 mg", "Blood Pressure", "An angiotensin receptor blocker commonly used for high blood pressure and related cardiovascular conditions."),
        ("ML-23", "Telmisartan 40 mg", "Blood Pressure", "An angiotensin receptor blocker commonly used for high blood pressure and selected cardiovascular indications."),
        ("ML-24", "Atorvastatin 10 mg", "Cholesterol", "A statin medicine used to lower LDL cholesterol and reduce cardiovascular risk."),
        ("ML-25", "Atorvastatin 20 mg", "Cholesterol", "A statin medicine used to lower LDL cholesterol and reduce cardiovascular risk."),
        ("ML-26", "Rosuvastatin 10 mg", "Cholesterol", "A statin medicine used to lower cholesterol and reduce cardiovascular risk."),
        ("ML-27", "Omeprazole 20 mg", "Digestive Care", "A proton-pump inhibitor that reduces stomach acid and is used for several acid-related conditions."),
        ("ML-28", "Pantoprazole 40 mg", "Digestive Care", "A proton-pump inhibitor that reduces stomach acid and is used for several acid-related conditions."),
        ("ML-29", "Esomeprazole 40 mg", "Digestive Care", "A proton-pump inhibitor that reduces stomach acid and is used for several acid-related conditions."),
        ("ML-30", "Famotidine 20 mg", "Digestive Care", "An H2-receptor blocker that reduces stomach acid and is used for acid-related conditions."),
        ("ML-31", "Ondansetron 4 mg", "Digestive Care", "A medicine used to help prevent and treat nausea and vomiting in specified clinical settings."),
        ("ML-32", "Domperidone 10 mg", "Digestive Care", "A medicine used in selected settings for nausea and vomiting; use should follow local medical guidance."),
        ("ML-33", "Diclofenac 50 mg", "Fever & Pain", "A nonsteroidal anti-inflammatory medicine used for pain and inflammation."),
        ("ML-34", "Naproxen 250 mg", "Fever & Pain", "A nonsteroidal anti-inflammatory medicine used for pain and inflammation."),
        ("ML-35", "Montelukast 10 mg", "Respiratory", "A leukotriene receptor antagonist used for selected asthma and allergy indications."),
        ("ML-36", "Salbutamol 4 mg", "Respiratory", "A bronchodilator used to relieve bronchospasm in conditions such as asthma."),
        ("ML-37", "Budesonide 200 mcg", "Respiratory", "A corticosteroid used in inhaled formulations for control of airway inflammation."),
        ("ML-38", "Levothyroxine 50 mcg", "Thyroid", "A thyroid hormone replacement medicine used to treat hypothyroidism."),
        ("ML-39", "Levothyroxine 100 mcg", "Thyroid", "A thyroid hormone replacement medicine used to treat hypothyroidism."),
        ("ML-40", "Albendazole 400 mg", "Antiparasitic", "An anthelmintic medicine used to treat certain parasitic worm infections."),
        ("ML-41", "Mebendazole 100 mg", "Antiparasitic", "An anthelmintic medicine used to treat certain intestinal worm infections."),
        ("ML-42", "Loperamide 2 mg", "Digestive Care", "An antidiarrheal medicine that reduces intestinal motility and is used for short-term symptom control."),
        ("ML-43", "Lactulose", "Digestive Care", "An osmotic laxative used to treat constipation and for specific clinical indications."),
        ("ML-44", "Bisacodyl 5 mg", "Digestive Care", "A stimulant laxative used for short-term relief of constipation."),
        ("ML-45", "Ferrous Sulfate", "Vitamins & Minerals", "An iron supplement used to prevent or treat iron deficiency when appropriate."),
        ("ML-46", "Folic Acid 5 mg", "Vitamins & Minerals", "A folate supplement used for prevention or treatment of folate deficiency in appropriate settings."),
        ("ML-47", "Calcium + Vitamin D", "Vitamins & Minerals", "A calcium and vitamin D supplement used to support bone health when supplementation is appropriate."),
        ("ML-48", "Vitamin D3", "Vitamins & Minerals", "A vitamin D supplement used to prevent or treat vitamin D deficiency."),
        ("ML-49", "Clotrimazole 1%", "Skin Care", "An antifungal medicine used for certain superficial fungal skin infections."),
        ("ML-50", "Miconazole 2%", "Skin Care", "An antifungal medicine used for certain superficial fungal infections."),
        ("ML-51", "Hydrocortisone 1%", "Skin Care", "A topical corticosteroid used to reduce inflammation and itching in selected skin conditions."),
        ("ML-52", "Povidone-Iodine", "Antiseptic", "An antiseptic preparation used for skin cleansing and other appropriate topical applications."),
        ("ML-53", "Chlorhexidine", "Antiseptic", "An antiseptic used for skin cleansing and infection-control applications."),
        ("ML-54", "Acetylcysteine 600 mg", "Respiratory", "A mucolytic medicine used in selected respiratory conditions and other clinical settings."),
        ("ML-55", "Ambroxol 30 mg", "Respiratory", "A mucolytic medicine used to help loosen and clear respiratory mucus."),
        ("ML-56", "Dextromethorphan", "Cough & Cold", "A cough suppressant used for short-term relief of certain coughs."),
        ("ML-57", "Guaifenesin", "Cough & Cold", "An expectorant used to help loosen mucus in the airways.")
    ];

    public static async Task SeedAsync(InventoryDbContext db)
    {
        foreach (var item in Catalog)
        {
            var existing = await db.Medicines.FirstOrDefaultAsync(x => x.ExternalMedicineId == item.Id);
            var image = $"/api/medicines/catalog/placeholder?name={Uri.EscapeDataString(item.Name)}";

            if (existing is null)
            {
                db.Medicines.Add(new Medicine
                {
                    ExternalMedicineId = item.Id,
                    ExternalSource = "MediLinkCatalog",
                    Name = item.Name,
                    Description = item.Description,
                    Category = item.Category,
                    ImageUrl = image,
                    IsActive = true,
                    Price = 0,
                    StockQuantity = 0
                });
            }
            else
            {
                existing.ExternalSource = "MediLinkCatalog";
                existing.Name = item.Name;
                existing.Description = item.Description;
                existing.Category = item.Category;
                existing.ImageUrl = image;
                existing.IsActive = true;
            }
        }

        await db.SaveChangesAsync();
    }
}
