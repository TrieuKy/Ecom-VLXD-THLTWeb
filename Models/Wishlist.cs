using System.ComponentModel.DataAnnotations;

namespace TrieuDoanKy_W2.Models
{
    public class Wishlist
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public int ProductId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public ApplicationUser? User { get; set; }
        public Product? Product { get; set; }
    }
}
