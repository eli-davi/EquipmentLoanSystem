namespace EquipmentLoanSystem.Core.Entities
{
    /// <summary>דגם ציוד (למשל "מצלמת Canon EOS R6"). לכל דגם יש עותקים פיזיים.</summary>
    public class Item
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // many-to-many
        public ICollection<Category> Categories { get; set; } = new List<Category>();

        // one-to-many
        public ICollection<ItemCopy> Copies { get; set; } = new List<ItemCopy>();
    }
}
