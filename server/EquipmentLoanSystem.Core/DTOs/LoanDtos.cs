using System.ComponentModel.DataAnnotations;
using EquipmentLoanSystem.Core.Enums;

namespace EquipmentLoanSystem.Core.DTOs
{
    /// <summary>
    /// בקשת השאלה. מזהה החבר לא נשלח מהלקוח - הוא נלקח מה-JWT,
    /// כדי שמשתמש לא יוכל להשאיל בשם מישהו אחר.
    /// </summary>
    public class LoanRequestDto
    {
        [Required(ErrorMessage = "יש לציין מזהה פריט")]
        [Range(1, int.MaxValue, ErrorMessage = "מזהה פריט לא תקין")]
        public int? ItemId { get; set; }

        /// <summary>אופציונלי - עותק ספציפי. אם לא נשלח, המערכת בוחרת עותק פנוי.</summary>
        [Range(1, int.MaxValue)]
        public int? ItemCopyId { get; set; }
    }

    public class UpdateLoanStatusDto
    {
        [Required(ErrorMessage = "יש לציין סטטוס")]
        public LoanStatus? Status { get; set; }
    }

    public class LoanDto
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public int ItemCopyId { get; set; }
        public string SerialNumber { get; set; } = string.Empty;
        public int MemberId { get; set; }
        public DateTime LoanDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Message { get; set; }
    }
}
