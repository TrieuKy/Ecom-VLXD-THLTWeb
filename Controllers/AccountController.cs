using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TrieuDoanKy_W2.Data;
using TrieuDoanKy_W2.Models;
using TrieuDoanKy_W2.Models.ViewModels;
using TrieuDoanKy_W2.Services;

namespace TrieuDoanKy_W2.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public AccountController(UserManager<ApplicationUser> userManager,
                                 SignInManager<ApplicationUser> signInManager,
                                 ApplicationDbContext context,
                                 IEmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _emailService = emailService;
        }

        // ────── ĐĂNG NHẬP ──────
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid) return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
                return LocalRedirect(returnUrl ?? "/");

            if (result.IsLockedOut)
                ModelState.AddModelError("", "Tài khoản tạm thời bị khóa do đăng nhập sai quá nhiều lần.");
            else
                ModelState.AddModelError("", "Email hoặc mật khẩu không đúng.");

            return View(model);
        }

        // ────── ĐĂNG KÝ ──────
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                Address = model.Address,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                TempData["Success"] = $"Chào mừng {model.FullName} đã đăng ký thành công!";
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

        // ────── ĐĂNG XUẤT ──────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // ────── EXTERNAL LOGIN (Google / Facebook) ──────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string? returnUrl = null)
        {
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
        {
            if (remoteError != null)
            {
                TempData["Error"] = $"Lỗi từ nhà cung cấp: {remoteError}";
                return RedirectToAction(nameof(Login));
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
                return RedirectToAction(nameof(Login));

            var result = await _signInManager.ExternalLoginSignInAsync(
                info.LoginProvider, info.ProviderKey, isPersistent: false);

            if (result.Succeeded)
                return LocalRedirect(returnUrl ?? "/");

            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            var fullName = info.Principal.FindFirstValue(ClaimTypes.Name) ?? email ?? "Người dùng";

            if (email == null)
            {
                TempData["Error"] = "Không lấy được email từ tài khoản ngoài.";
                return RedirectToAction(nameof(Login));
            }

            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                await _userManager.AddLoginAsync(existingUser, info);
                await _signInManager.SignInAsync(existingUser, isPersistent: false);
                return LocalRedirect(returnUrl ?? "/");
            }

            var newUser = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(newUser);
            if (createResult.Succeeded)
            {
                await _userManager.AddLoginAsync(newUser, info);
                await _signInManager.SignInAsync(newUser, isPersistent: false);
                TempData["Success"] = $"Chào mừng {fullName}!";
                return LocalRedirect(returnUrl ?? "/");
            }

            TempData["Error"] = "Không thể tạo tài khoản. Vui lòng thử lại.";
            return RedirectToAction(nameof(Login));
        }

        // ────── THÔNG TIN TÀI KHOẢN ──────
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));

            var orders = await _context.Orders
                .Where(o => o.UserId == user.Id)
                .Include(o => o.OrderDetails!)
                .ThenInclude(od => od.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            ViewBag.Orders = orders;
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Profile(ApplicationUser model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));

            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            user.Address = model.Address;

            var result = await _userManager.UpdateAsync(user);
            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                ? "Cập nhật thông tin thành công!"
                : "Có lỗi xảy ra khi cập nhật thông tin.";

            return RedirectToAction(nameof(Profile));
        }

        // ────── CHI TIẾT ĐƠN HÀNG ──────
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> OrderDetail(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));

            var order = await _context.Orders
                .Include(o => o.OrderDetails!)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == user.Id);

            if (order == null) return NotFound();
            return View(order);
        }

        // ────── HỦY ĐƠN HÀNG ──────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));

            var order = await _context.Orders
                .Include(o => o.OrderDetails!)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == user.Id);

            if (order == null) return NotFound();

            if (order.Status != "Chờ xác nhận")
            {
                TempData["Error"] = "Chỉ có thể hủy đơn hàng ở trạng thái \"Chờ xác nhận\".";
                return RedirectToAction(nameof(OrderDetail), new { id });
            }

            // Hoàn kho
            foreach (var detail in order.OrderDetails ?? new())
            {
                if (detail.Product != null)
                    detail.Product.Stock += detail.Quantity;
            }

            order.Status = "Đã hủy";
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã hủy đơn hàng #{id} thành công.";
            return RedirectToAction(nameof(Profile));
        }

        // ────── QUÊN MẬT KHẨU ──────
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetLink = Url.Action(nameof(ResetPassword), "Account",
                    new { email = model.Email, token }, Request.Scheme)!;
                await _emailService.SendPasswordResetAsync(user.Email!, user.FullName ?? user.Email!, resetLink);
            }

            TempData["Success"] = "Nếu email tồn tại trong hệ thống, chúng tôi đã gửi link đặt lại mật khẩu.";
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult ResetPassword(string email, string token)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
                return RedirectToAction(nameof(Login));

            return View(new ResetPasswordViewModel { Email = email, Token = token });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError("", error.Description);
                    return View(model);
                }
            }

            TempData["Success"] = "Đặt lại mật khẩu thành công! Vui lòng đăng nhập.";
            return RedirectToAction(nameof(Login));
        }
    }
}
