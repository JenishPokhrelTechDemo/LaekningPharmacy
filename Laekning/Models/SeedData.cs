using Microsoft.EntityFrameworkCore;

namespace Laekning.Models {

    public static class SeedData {

        public static void EnsurePopulated(IApplicationBuilder app) {
            StoreDbContext context = app.ApplicationServices
                .CreateScope().ServiceProvider
                .GetRequiredService<StoreDbContext>();

            if (context.Database.GetPendingMigrations().Any()) {
                context.Database.Migrate();
            }

            if (!context.Products.Any()) {
                context.Products.AddRange(
                    new Product {
                        Name = "Imodium",
                        Description = "Consume in case of excruciating diarrhea.",
                        Category = "Antidiarrheal",
                        Price = 8m,
                        Image = "imodium.jpg"
                    },
                    new Product {
                        Name = "Aspirin",
                        Description = "The most well known analgesic",
                        Category = "Analgesic",
                        Price = 8m,
                        Image = "aspirin.jpg"
                    },
                    new Product {
                        Name = "Tums",
                        Description = "Chewable medicine for treating short term acidity.",
                        Category = "Antacid",
                        Price = 14m,
                        Image = "tums.jpg"
                    },
                    new Product {
                        Name = "Amitriptyline",
                        Description = "An antidepressant. Use only under the guidance of a psychatrist.",
                        Category = "Antidepressant",
                        Price = 27m,
                        Image = "amitriptyline.jpg"
                    },
                    new Product {
                        Name = "Tylenol",
                        Description = "Branded acetaminophen, a painkiller from America's McNeil Consumer Healthcare.",
                        Category = "Analgesic",
                        Price = 2m,
                        Image = "tylenol.jpg"
                    },
                    new Product {
                        Name = "Ibuprofen",
                        Description = "An over the top antipyretic(fever reducer). Available variants include Advil and Motrin.",
                        Category = "Antipyretic",
                        Price = 15m,
                        Image = "ibuprofen.jpg"
                    },
                    new Product {
                        Name = "Milk of Magnesia",
                        Description = "Useful for heartburn and indigestion caused by stomach acid problems.",
                        Category = "Antacid",
                        Price = 24m,
                        Image = "milk_of_magnesia.jpg"
                    },
                    new Product {
                        Name = "Mucinex",
                        Description = "Get 12 hours of relief from even the worst cold, flue and sinus symptoms.",
                        Category = "Cold reducer",
                        Price = 24m,
                        Image = "mucinex.jpg"
                    },
                    new Product {
                        Name = "Amoxillin",
                        Description = "Used to treat the symptoms of too much stomach acid such as stomach upset, heartburn, and acid indigestion.",
                        Category = "Antacid",
                        Price = 10m,
                        Image = "amoxicillin.jpg"
                    },
                    new Product {
                        Name = "Amphojel",
                        Description = "Used to treat certain infections caused by bacteria, such as pneumonia; bronchitis and infections of the ears, nose, throat, urinary tract, and skin.",
                        Category = "Antibiotic",
                        Price = 5m,
                        Image = "amphojel.jpg"
                    },
                    new Product {
                        Name = "Pantoprazole",
                        Description = "Pantoprazole is used to treat certain conditions in which there is too much acid in the stomach.",
                        Category = "Antacid",
                        Price = 450m,
                        Image = "pantoprazole.jpg"
                    },
                    new Product {
                        Name = "Tincture Belladonna",
                        Description = "Effective in the treatment of spasms of the gastrointestinal tracts.",
                        Category = "Antacid",
                        Price = 15m,
                        Image = "belladonna.jpeg"
                    },
                    new Product {
                        Name = "Diclofenac",
                        Description = "Diclofenac is a nonsteroidal anti-inflammatory drug (NSAID) used to treat mild-to-moderate pain.",
                        Category = "Analgesic",
                        Price = 15m,
                        Image = "diclofenac.jpeg"
                    },
                    new Product {
                        Name = "Humalog",
                        Description = "Humalog is a rapid-acting insulin analog used to manage blood sugar levels in people with diabetes.",
                        Category = "Anti diabetic",
                        Price = 9m,
                        Image = "humalog.jpg"
                    },
                    new Product {
                        Name = "Lipitor",
                        Description = "Atorvastatin, sold under the brand name Lipitor among others, is a statin medication used to prevent cardiovascular disease in those at high risk and to treat abnormal lipid levels.",
                        Category = "Antihyperlipidemic",
                        Price = 90m,
                        Image = "lipitor.jpeg"
                    },
                    new Product {
                        Name = "Atenolol",
                        Description = "Atenolol is used alone or in combination with other medications to treat high blood pressure.",
                        Category = "Antihypertensive",
                        Price = 20m,
                        Image = "atenolol.jpeg"
                    },
                    new Product {
                        Name = "Zestril",
                        Description = "Zestril (lisinopril) is used alone or in combination with other medications to treat high blood pressure in adults and children 6 years of age and older.",
                        Category = "Antihypertensive",
                        Price = 10m,
                        Image = "zestril.jpeg"
                    },
                    new Product {
                        Name = "Ferrous Sulphate",
                        Description = "Ferrous sulfate (or sulphate) is a type of iron that's used as a medicine to treat and prevent iron deficiency anaemia.",
                        Category = "Antianemic",
                        Price = 10m,
                        Image = "ferrous_sulphate_folic.jpeg"
                    },
                    new Product {
                        Name = "Humulin N",
                        Description = "Humulin N is used to improve blood sugar control in adults and children with diabetes mellitus.",
                        Category = "Anti diabetic",
                        Price = 12m,
                        Image = "Humulin_N.jpeg"
                    },
                    new Product {
                        Name = "Acetaminophen",
                        Description = "Acetaminophen, commonly known as paracetamol is a common painkiller used to treat aches and pain. It can also be used to reduce a high temperature.",
                        Category = "Antipyretic",
                        Price = 10m,
                        Image = "Acetaminophen.jpeg"
                    },
                    new Product {
                        Name = "Metoclopramide",
                        Description = "Metoclopramide is a dopamine receptor antagonist and has been approved by the FDA to treat nausea and vomiting in patients with gastroesophageal reflux disease or diabetic gastroparesis by increasing gastric motility.",
                        Category = "Antiemetic",
                        Price = 17m,
                        Image = "metoclopramide.jpeg"
                    }
                );
                context.SaveChanges();
            }
        }
    }
}
