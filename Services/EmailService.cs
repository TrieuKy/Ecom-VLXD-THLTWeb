using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace TrieuDoanKy_W2.Services
{
    public interface IEmailService
    {
        Task SendOrderConfirmationAsync(string toEmail, string toName, int orderId,
            string productName, int quantity, decimal totalPrice, string shippingAddress);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendOrderConfirmationAsync(string toEmail, string toName, int orderId,
            string productName, int quantity, decimal totalPrice, string shippingAddress)
        {
            try
            {
                var smtpHost = _config["Email:SmtpHost"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_config["Email:SmtpPort"] ?? "587");
                var senderEmail = _config["Email:SenderEmail"] ?? "";
                var senderName = _config["Email:SenderName"] ?? "VLXD Store";
                var password = _config["Email:Password"] ?? "";

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(senderName, senderEmail));
                message.To.Add(new MailboxAddress(toName, toEmail));
                message.Subject = $"✅ Xác nhận đơn hàng #{orderId} - VLXD Shop";

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = BuildEmailHtml(toName, orderId, productName, quantity, totalPrice, shippingAddress)
                };
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(senderEmail, password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi email xác nhận đơn hàng #{OrderId}", orderId);
            }
        }

        private static string BuildEmailHtml(string name, int orderId, string productName,
            int quantity, decimal totalPrice, string shippingAddress)
        {
            return $"""
            <!DOCTYPE html>
            <html lang="vi">
            <head><meta charset="UTF-8"></head>
            <body style="font-family:'Segoe UI',sans-serif;background:#f5f6fa;margin:0;padding:20px;">
              <div style="max-width:600px;margin:0 auto;background:white;border-radius:12px;overflow:hidden;box-shadow:0 4px 20px rgba(0,0,0,.1)">
                <div style="background:linear-gradient(135deg,#e65c00,#cc4e00);padding:30px;text-align:center">
                  <h1 style="color:white;margin:0;font-size:24px">🏗️ VLXD Shop</h1>
                  <p style="color:rgba(255,255,255,.9);margin:8px 0 0">Xác nhận đơn hàng thành công</p>
                </div>
                <div style="padding:30px">
                  <p style="font-size:16px">Xin chào <strong>{name}</strong>,</p>
                  <p>Cảm ơn bạn đã đặt hàng tại <strong>VLXD Shop</strong>. Đơn hàng của bạn đã được tiếp nhận!</p>
                  <div style="background:#f8f9fa;border-radius:8px;padding:20px;margin:20px 0;border-left:4px solid #e65c00">
                    <h3 style="margin:0 0 15px;color:#1a1a2e">📋 Chi tiết đơn hàng #{orderId}</h3>
                    <table style="width:100%;border-collapse:collapse">
                      <tr><td style="padding:8px 0;color:#666">Sản phẩm:</td><td style="font-weight:600">{productName}</td></tr>
                      <tr><td style="padding:8px 0;color:#666">Số lượng:</td><td><strong>{quantity}</strong></td></tr>
                      <tr><td style="padding:8px 0;color:#666">Tổng tiền:</td><td><strong style="color:#e65c00;font-size:18px">{totalPrice:N0} ₫</strong></td></tr>
                      <tr><td style="padding:8px 0;color:#666">Địa chỉ:</td><td>{shippingAddress}</td></tr>
                      <tr><td style="padding:8px 0;color:#666">Trạng thái:</td><td><span style="background:#d4edda;color:#155724;padding:3px 10px;border-radius:20px;font-size:13px">Chờ xác nhận</span></td></tr>
                    </table>
                  </div>
                  <p style="color:#666;font-size:14px">Chúng tôi sẽ liên hệ xác nhận trong vòng <strong>24 giờ</strong>. Hotline: <strong>1800-1234</strong></p>
                </div>
                <div style="background:#1a1a2e;padding:15px;text-align:center">
                  <p style="color:#aaa;margin:0;font-size:13px">© 2026 VLXD Shop · 123 Đường Xây Dựng, Q.1, TP.HCM</p>
                </div>
              </div>
            </body>
            </html>
            """;
        }
    }
}
