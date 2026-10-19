using EquipmentLoanSystem.Core.Enums;

namespace EquipmentLoanSystem.Core.Entities
{
    public class Loan
    {
        public int Id { get; set; }

        public int ItemCopyId { get; set; }
        public ItemCopy ItemCopy { get; set; } = null!;

        public int MemberId { get; set; }
        public Member Member { get; set; } = null!;

        // כל התאריכים ב-UTC
        public DateTime LoanDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }

        public LoanStatus Status { get; set; } = LoanStatus.Active;
    }
}
