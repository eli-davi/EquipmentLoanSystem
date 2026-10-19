using System.ComponentModel.DataAnnotations;

namespace EquipmentLoanSystem.Core.DTOs
{
    // ---------- Response ----------
    public class ItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Categories { get; set; } = new();
        public int TotalCopies { get; set; }
        public int AvailableCopies { get; set; }
        public bool IsAvailable => AvailableCopies > 0;
    }

    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    // ---------- Requests ----------
    public class ItemQueryDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "מספר העמוד חייב להיות 1 ומעלה")]
        public int Page { get; set; } = 1;

        [Range(1, 50, ErrorMessage = "גודל עמוד חייב להיות בין 1 ל-50")]
        public int PageSize { get; set; } = 6;

        [MaxLength(100)]
        public string? Search { get; set; }

        public int? CategoryId { get; set; }

        /// <summary>name | id | available</summary>
        [RegularExpression("^(name|id|available)$", ErrorMessage = "ניתן למיין לפי name, id או available")]
        public string SortBy { get; set; } = "id";

        public bool Descending { get; set; }

        public bool OnlyAvailable { get; set; }
    }

    public class CreateItemDto
    {
        [Required(ErrorMessage = "שם הפריט הוא שדה חובה")]
        [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public List<int> CategoryIds { get; set; } = new();

        [Range(1, 20, ErrorMessage = "מספר העותקים חייב להיות בין 1 ל-20")]
        public int NumberOfCopies { get; set; } = 1;
    }

    public class UpdateItemDto
    {
        [Required(ErrorMessage = "שם הפריט הוא שדה חובה")]
        [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public List<int> CategoryIds { get; set; } = new();
    }
}
