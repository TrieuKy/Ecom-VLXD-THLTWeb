using System.ComponentModel.DataAnnotations;

namespace TrieuDoanKy_W2.Models
{
    public class ProductReview
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public int ProductId { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(1000)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public ApplicationUser? User { get; set; }
        public Product? Product { get; set; }
    }
}
