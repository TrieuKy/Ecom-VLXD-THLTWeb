using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrieuDoanKy_W2.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        [Required, StringLength(50)]
        public string Status { get; set; } = "Chờ xác nhận";

        [Required, StringLength(255)]
        public string ShippingAddress { get; set; } = string.Empty;

        [Required, StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string PaymentMethod { get; set; } = "COD";

        // Navigation
        public ApplicationUser? User { get; set; }
        public List<OrderDetail>? OrderDetails { get; set; }
    }

    public class OrderDetail
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        // Navigation
        public Order? Order { get; set; }
        public Product? Product { get; set; }
    }

    public class ShoppingCartItem
    {
        public int Id { get; set; }

        [Required]
        public string ApplicationUserId { get; set; } = string.Empty;

        public int ProductId { get; set; }
        public int Quantity { get; set; }

        // Navigation
        public Product? Product { get; set; }
    }

    public class ProductImage
    {
        public int Id { get; set; }
        public int ProductId { get; set; }

        [Required, StringLength(500)]
        public string ImageUrl { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Caption { get; set; }

        public int? SortOrder { get; set; }
        public DateTime? CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public Product? Product { get; set; }
    }
}
