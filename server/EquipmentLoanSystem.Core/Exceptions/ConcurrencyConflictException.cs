namespace EquipmentLoanSystem.Core.Exceptions
{
    /// <summary>
    /// חריגה של שכבת ה-Core שמייצגת התנגשות אופטימית.
    /// שכבת ה-Data תופסת DbUpdateConcurrencyException (חריגה של EF Core) ועוטפת אותה כאן,
    /// כך ששכבת ה-Service לא צריכה תלות ב-EF Core.
    /// </summary>
    public class ConcurrencyConflictException : Exception
    {
        public ConcurrencyConflictException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
