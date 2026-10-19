namespace EquipmentLoanSystem.Core.Settings
{
    /// <summary>
    /// נטען מ-IConfiguration דרך IOptions. ה-Secret לא נשמר ב-repo:
    /// בפיתוח הוא מגיע מ-User Secrets (ואם לא הוגדר - נוצר מפתח אקראי בזמן עלייה).
    /// </summary>
    public class JwtSettings
    {
        public const string SectionName = "JwtSettings";

        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public string Secret { get; set; } = string.Empty;
        public int ExpiryInMinutes { get; set; } = 120;
    }
}
