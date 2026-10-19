namespace EquipmentLoanSystem.Core.DTOs
{
    /// <summary>מבנה JSON אחיד לכל תשובת שגיאה (מה-Controllers ומה-ErrorHandlingMiddleware).</summary>
    public class ErrorResponseDto
    {
        public int Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? CorrelationId { get; set; }
    }
}
