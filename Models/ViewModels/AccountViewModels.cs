using System.ComponentModel.DataAnnotations;

namespace TrieuDoanKy_W2.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [StringLength(50)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
    }

    public class OrderViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public string? Unit { get; set; }
        public int Stock { get; set; }

        // Thông tin người nhận
        [Required(ErrorMessage = "Vui lòng nhập họ tên người nhận")]
        [StringLength(100)]
        public string ReceiverName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string PhoneNumber { get; set; } = string.Empty;

        // Địa chỉ giao hàng chi tiết
        [Required(ErrorMessage = "Vui lòng nhập số nhà, tên đường")]
        public string AddressLine { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập phường/xã")]
        public string Ward { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập quận/huyện")]
        public string District { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn tỉnh/thành phố")]
        public string Province { get; set; } = string.Empty;

        // Địa chỉ đầy đủ (tự ghép để lưu vào Order.ShippingAddress)
        public string ShippingAddress => $"{AddressLine}, {Ward}, {District}, {Province}";

        [Range(1, 9999, ErrorMessage = "Số lượng phải từ 1 trở lên")]
        public int Quantity { get; set; } = 1;

        public string PaymentMethod { get; set; } = "COD";

        public string? OrderNote { get; set; }
    }

    public class CartViewModel
    {
        public List<ShoppingCartItem> Items { get; set; } = new();
        public decimal TotalPrice => Items.Sum(i => (i.Product?.Price ?? 0) * i.Quantity);
    }

    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordViewModel
    {
        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
