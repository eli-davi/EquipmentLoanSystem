namespace EquipmentLoanSystem.Core.Entities
{
    public class Member
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = Constants.Roles.User;

        public ICollection<Loan> Loans { get; set; } = new List<Loan>();
    }
}
