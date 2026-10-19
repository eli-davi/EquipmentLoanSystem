using System.ComponentModel.DataAnnotations;

namespace EquipmentLoanSystem.Core.DTOs
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "אימייל הוא שדה חובה")]
        [EmailAddress(ErrorMessage = "כתובת אימייל לא תקינה")]
        [MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "סיסמה היא שדה חובה")]
        [MinLength(6, ErrorMessage = "הסיסמה חייבת להכיל לפחות 6 תווים")]
        [MaxLength(100)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "שם פרטי הוא שדה חובה")]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "שם משפחה הוא שדה חובה")]
        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        // אין כאן Role בכוונה: משתמש לא יכול לבחור לעצמו הרשאת Admin בהרשמה.
    }

    public class LoginDto
    {
        [Required(ErrorMessage = "אימייל הוא שדה חובה")]
        [EmailAddress(ErrorMessage = "כתובת אימייל לא תקינה")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "סיסמה היא שדה חובה")]
        public string Password { get; set; } = string.Empty;
    }

    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public int MemberId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}
