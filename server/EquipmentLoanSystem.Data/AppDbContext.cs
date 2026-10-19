using EquipmentLoanSystem.Core.Constants;
using EquipmentLoanSystem.Core.Entities;
using EquipmentLoanSystem.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace EquipmentLoanSystem.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Item> Items => Set<Item>();
        public DbSet<ItemCopy> ItemCopies => Set<ItemCopy>();
        public DbSet<Member> Members => Set<Member>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Loan> Loans => Set<Loan>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ---------- Item ----------
            modelBuilder.Entity<Item>(e =>
            {
                e.Property(i => i.Name).IsRequired().HasMaxLength(100);
                e.Property(i => i.Description).HasMaxLength(500);
                e.HasIndex(i => i.Name);

                // one-to-many: פריט -> עותקים
                e.HasMany(i => i.Copies)
                 .WithOne(c => c.Item)
                 .HasForeignKey(c => c.ItemId)
                 .OnDelete(DeleteBehavior.Cascade);

                // many-to-many: פריט <-> קטגוריה (טבלת קישור אוטומטית + Seed)
                e.HasMany(i => i.Categories)
                 .WithMany(c => c.Items)
                 .UsingEntity(j => j.HasData(SeedData.ItemCategories));
            });

            // ---------- Category ----------
            modelBuilder.Entity<Category>(e =>
            {
                e.Property(c => c.Name).IsRequired().HasMaxLength(50);
                e.HasIndex(c => c.Name).IsUnique();
            });

            // ---------- ItemCopy (המשאב המוגבל) ----------
            modelBuilder.Entity<ItemCopy>(e =>
            {
                e.Property(c => c.SerialNumber).IsRequired().HasMaxLength(50);
                e.HasIndex(c => c.SerialNumber).IsUnique();
                e.HasIndex(c => new { c.ItemId, c.Status });

                // Optimistic concurrency: הערך הישן נכנס לתנאי ה-WHERE בכל UPDATE
                e.Property(c => c.Version).IsConcurrencyToken();
            });

            // ---------- Member ----------
            modelBuilder.Entity<Member>(e =>
            {
                e.Property(m => m.Email).IsRequired().HasMaxLength(200);
                e.HasIndex(m => m.Email).IsUnique();
                e.Property(m => m.FirstName).IsRequired().HasMaxLength(50);
                e.Property(m => m.LastName).IsRequired().HasMaxLength(50);
                e.Property(m => m.PasswordHash).IsRequired().HasMaxLength(200);
                e.Property(m => m.Role).IsRequired().HasMaxLength(20);
            });

            // ---------- Loan ----------
            modelBuilder.Entity<Loan>(e =>
            {
                e.HasOne(l => l.ItemCopy)
                 .WithMany(c => c.Loans)
                 .HasForeignKey(l => l.ItemCopyId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(l => l.Member)
                 .WithMany(m => m.Loans)
                 .HasForeignKey(l => l.MemberId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasIndex(l => new { l.MemberId, l.Status });
                
                // הגנה נוספת ברמת בסיס הנתונים: לכל עותק לכל היותר השאלה פעילה אחת
                e.HasIndex(l => l.ItemCopyId)
                 .IsUnique()
                 .HasFilter("[Status] = 0")
                 .HasDatabaseName("IX_Loans_ItemCopyId_ActiveUnique");
            });

            // ---------- Seed: כל הציוד מוגדר כאן בקוד ----------
            modelBuilder.Entity<Category>().HasData(SeedData.Categories);
            modelBuilder.Entity<Item>().HasData(SeedData.Items);
            modelBuilder.Entity<ItemCopy>().HasData(SeedData.Copies);
            modelBuilder.Entity<Member>().HasData(SeedData.Members);
            modelBuilder.Entity<Loan>().HasData(SeedData.Loans);
        }

        /// <summary>
        /// מחליף את ה-Version של כל עותק שהשתנה, כדי שאף Service לא יצטרך לזכור לעשות זאת.
        /// EF משתמש בערך המקורי (שנקרא מה-DB) בתנאי ה-WHERE, ובערך החדש ב-SET.
        /// </summary>
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            RenewConcurrencyTokens();
            return base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges()
        {
            RenewConcurrencyTokens();
            return base.SaveChanges();
        }

        private void RenewConcurrencyTokens()
        {
            foreach (var entry in ChangeTracker.Entries<ItemCopy>().Where(e => e.State == EntityState.Modified))
            {
                entry.Entity.Version = Guid.NewGuid();
            }
        }
    }

    /// <summary>
    /// נתוני Seed. המערכת עולה מיד אחרי יצירת בסיס הנתונים עם ציוד, קטגוריות ומשתמשי דמו.
    /// כל הערכים קבועים (כולל Guid ותאריכים) כדי ש-HasData לא ייצר migration חדש בכל פעם.
    /// </summary>
    public static class SeedData
    {
        public static readonly Category[] Categories =
        {
            new() { Id = 1, Name = "צילום" },
            new() { Id = 2, Name = "הקרנה" },
            new() { Id = 3, Name = "שמע" },
            new() { Id = 4, Name = "תאורה" },
            new() { Id = 5, Name = "מחשוב" }
        };

        public static readonly Item[] Items =
        {
            new() { Id = 1, Name = "מצלמת Canon EOS R6", Description = "מצלמה מקצועית ללא מראה, כולל עדשת 24-105" },
            new() { Id = 2, Name = "מקרן Epson Full HD", Description = "מקרן 3,500 לומן עם כבל HDMI" },
            new() { Id = 3, Name = "ערכת סאונד ניידת JBL", Description = "רמקול נייד עם סוללה ל-12 שעות" },
            new() { Id = 4, Name = "תאורת סטודיו Godox", Description = "זוג פנסי LED עם חצובות" },
            new() { Id = 5, Name = "מחשב נייד Dell Latitude", Description = "מחשב נייד 14 אינץ' להצגות ולעבודה" },
            new() { Id = 6, Name = "חצובה מקצועית Manfrotto", Description = "חצובת אלומיניום עם ראש נע" },
            new() { Id = 7, Name = "מיקרופון אלחוטי Shure", Description = "מיקרופון יד אלחוטי עם מקלט" },
            new() { Id = 8, Name = "מסך הקרנה נייד 100 אינץ'", Description = "מסך על חצובה, מתקפל לתיק נשיאה" }
        };

        // טבלת הקישור של ה-many-to-many (שמות העמודות לפי הקונבנציה של EF: ItemsId, CategoriesId)
        public static readonly object[] ItemCategories =
        {
            new { ItemsId = 1, CategoriesId = 1 },
            new { ItemsId = 2, CategoriesId = 2 },
            new { ItemsId = 3, CategoriesId = 3 },
            new { ItemsId = 4, CategoriesId = 4 },
            new { ItemsId = 4, CategoriesId = 1 },
            new { ItemsId = 5, CategoriesId = 5 },
            new { ItemsId = 5, CategoriesId = 2 },
            new { ItemsId = 6, CategoriesId = 1 },
            new { ItemsId = 7, CategoriesId = 3 },
            new { ItemsId = 8, CategoriesId = 2 }
        };

        // עותק פיזי אחד לכל פריט: לחיצה ראשונה - "הפריט הושאל בהצלחה", לחיצה נוספת - "זה כבר מושאל".
        public static readonly ItemCopy[] Copies =
        {
            new() { Id = 1, ItemId = 1, SerialNumber = "SN-001", Status = CopyStatus.Available, Version = new Guid("6BB3B812-1BC7-47ED-94F6-B357E06A9159") },
            // המקרן מושאל כבר ב-Seed (ע"י משתמש הדמו השני) - להדגמה מיידית של "זה כבר מושאל"
            new() { Id = 2, ItemId = 2, SerialNumber = "SN-002", Status = CopyStatus.Loaned,    Version = new Guid("889DD2B4-A2CC-4F5F-9F8F-CC2266487F13") },
            new() { Id = 3, ItemId = 3, SerialNumber = "SN-003", Status = CopyStatus.Available, Version = new Guid("FC68DE6B-F02A-4EEA-AE84-89E7D0B5491C") },
            new() { Id = 4, ItemId = 4, SerialNumber = "SN-004", Status = CopyStatus.Available, Version = new Guid("46E85045-553C-4E51-B3ED-17BE9B7D064D") },
            new() { Id = 5, ItemId = 5, SerialNumber = "SN-005", Status = CopyStatus.Available, Version = new Guid("438E13FE-949F-45D7-8D66-C8D6172C3771") },
            new() { Id = 6, ItemId = 6, SerialNumber = "SN-006", Status = CopyStatus.Available, Version = new Guid("FCD0BA9C-B3E4-49CA-8E3B-B6E7A4C0084B") },
            new() { Id = 7, ItemId = 7, SerialNumber = "SN-007", Status = CopyStatus.Available, Version = new Guid("86745196-A6F3-42B6-B22E-A9FA1E20D04F") },
            new() { Id = 8, ItemId = 8, SerialNumber = "SN-008", Status = CopyStatus.Available, Version = new Guid("12A006E2-472B-4228-8504-6344B04CF1C2") }
        };

        // סיסמת כל משתמשי הדמו: 123456 (נשמרת כ-hash של PBKDF2-SHA256 בלבד)
        public static readonly Member[] Members =
        {
            new() { Id = 1, FirstName = "מנהל", LastName = "מערכת", Email = "admin@example.com", Role = Roles.Admin,
                    PasswordHash = "PBKDF2$100000$qQlWURpgU+3PvgI+keKZ8g==$8XCeZ8DkM3XHvtGFCwzb/Z42BSTY6Wre9WQqR4DcuQQ=" },
            new() { Id = 2, FirstName = "משתמש", LastName = "ראשון", Email = "user@example.com", Role = Roles.User,
                    PasswordHash = "PBKDF2$100000$UtQAw7uQ8bdDoHRvP4yx5w==$XMK0VkjswHNCUoUrXxNuTjSJnhq1l2Z3ZLHUug4yZjI=" },
            new() { Id = 3, FirstName = "משתמש", LastName = "שני", Email = "user2@example.com", Role = Roles.User,
                    PasswordHash = "PBKDF2$100000$/cLNS0FO2Isf3FtSBPTV+Q==$Mxe7oZ73f4DSQFd1Qc3PoEjXxHLOad+wjLObLZs7TpE=" }
        };

        public static readonly Loan[] Loans =
        {
            new() { Id = 1, ItemCopyId = 2, MemberId = 3, Status = LoanStatus.Active,
                    LoanDate = new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc),
                    DueDate  = new DateTime(2026, 12, 31, 9, 0, 0, DateTimeKind.Utc) }
        };
    }
}
