using System.ComponentModel.DataAnnotations;

namespace TrieuDoanKy_W2.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        public DateTime? CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public List<Product>? Products { get; set; }
    }
}
