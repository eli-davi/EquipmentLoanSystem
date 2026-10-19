using EquipmentLoanSystem.Core.Enums;

namespace EquipmentLoanSystem.Core.Entities
{
    /// <summary>
    /// עותק פיזי של פריט - זה המשאב המוגבל שעליו מתחרים.
    /// </summary>
    public class ItemCopy
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public Item Item { get; set; } = null!;

        public string SerialNumber { get; set; } = string.Empty;
        public CopyStatus Status { get; set; } = CopyStatus.Available;

        /// <summary>
        /// Concurrency token. מוגדר ב-Fluent API (IsConcurrencyToken) ומוחלף בכל שמירה
        /// בדריסה של SaveChangesAsync ב-AppDbContext. EF מוסיף את הערך הישן ל-WHERE של ה-UPDATE.
        /// </summary>
        public Guid Version { get; set; } = Guid.NewGuid();

        public ICollection<Loan> Loans { get; set; } = new List<Loan>();
    }
}
