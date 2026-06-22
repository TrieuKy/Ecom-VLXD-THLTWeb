using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrieuDoanKy_W2.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }

        public string? Description { get; set; }

        public int Stock { get; set; }

        [StringLength(50)]
        public string? Unit { get; set; }

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        public int CategoryId { get; set; }

        public DateTime? CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // Stored as JSON string in DB, helper property
        public string? ImageUrls { get; set; }

        // Navigation
        public Category? Category { get; set; }
        public List<ProductImage>? ProductImages { get; set; }
        public List<OrderDetail>? OrderDetails { get; set; }
    }
}
