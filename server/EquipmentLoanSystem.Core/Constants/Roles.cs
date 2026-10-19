namespace EquipmentLoanSystem.Core.Constants
{
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string User = "User";
    }

    /// <summary>שמות ה-claims שנכתבים ל-JWT ונקראים ממנו.</summary>
    public static class AppClaimTypes
    {
        public const string MemberId = "sub";
        public const string Email = "email";
        public const string Name = "name";
        public const string Role = "role";
    }
}
