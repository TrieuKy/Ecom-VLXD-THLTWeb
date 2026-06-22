using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrieuDoanKy_W2.Data;
using TrieuDoanKy_W2.Models;

namespace TrieuDoanKy_W2.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // ── KPI Cards ──
            var totalRevenue  = await _context.Orders.Where(o => o.Status == "Hoàn thành").SumAsync(o => o.TotalPrice);
            var totalOrders   = await _context.Orders.CountAsync();
            var totalProducts = await _context.Products.CountAsync();
            var totalUsers    = await _context.Users.CountAsync();

            ViewBag.TotalRevenue  = totalRevenue;
            ViewBag.TotalOrders   = totalOrders;
            ViewBag.TotalProducts = totalProducts;
            ViewBag.TotalUsers    = totalUsers;

            // ── Order status breakdown ──
            ViewBag.OrdersPending   = await _context.Orders.CountAsync(o => o.Status == "Chờ xác nhận");
            ViewBag.OrdersShipping  = await _context.Orders.CountAsync(o => o.Status == "Đang giao");
            ViewBag.OrdersDone      = await _context.Orders.CountAsync(o => o.Status == "Hoàn thành");
            ViewBag.OrdersCancelled = await _context.Orders.CountAsync(o => o.Status == "Đã hủy");

            // ── Revenue chart: last 6 months ──
            var sixMonthsAgo = DateTime.Now.AddMonths(-5);
            var monthlyRevenue = await _context.Orders
                .Where(o => o.Status == "Hoàn thành" && o.OrderDate >= sixMonthsAgo)
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Revenue = g.Sum(o => o.TotalPrice) })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();

            var chartLabels = new List<string>();
            var chartData   = new List<decimal>();
            for (int i = 5; i >= 0; i--)
            {
                var d = DateTime.Now.AddMonths(-i);
                chartLabels.Add($"T{d.Month}/{d.Year % 100}");
                var found = monthlyRevenue.FirstOrDefault(x => x.Year == d.Year && x.Month == d.Month);
                chartData.Add(found?.Revenue ?? 0);
            }
            ViewBag.ChartLabels = System.Text.Json.JsonSerializer.Serialize(chartLabels);
            ViewBag.ChartData   = System.Text.Json.JsonSerializer.Serialize(chartData);

            // ── Low stock products (≤ 10) ──
            ViewBag.LowStockProducts = await _context.Products
                .Where(p => p.Stock <= 10)
                .OrderBy(p => p.Stock)
                .Take(5)
                .ToListAsync();

            // ── Top selling products ──
            ViewBag.TopProducts = await _context.OrderDetails
                .Include(od => od.Product)
                .GroupBy(od => od.Product!.Name)
                .Select(g => new { Name = g.Key, TotalSold = g.Sum(od => od.Quantity) })
                .OrderByDescending(x => x.TotalSold)
                .Take(5)
                .ToListAsync();

            // ── Recent orders (with user info) ──
            var recentOrders = await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .Take(8)
                .ToListAsync();

            return View(recentOrders);
        }

        public async Task<IActionResult> Orders()
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails!)
                    .ThenInclude(od => od.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            ViewBag.TotalPending   = orders.Count(o => o.Status == "Chờ xác nhận");
            ViewBag.TotalShipping  = orders.Count(o => o.Status == "Đang giao");
            ViewBag.TotalDone      = orders.Count(o => o.Status == "Hoàn thành");
            ViewBag.TotalCancelled = orders.Count(o => o.Status == "Đã hủy");

            return View(orders);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(int id, string status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                order.Status = status;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Cập nhật đơn #{id} → \"{status}\" thành công!";
            }
            return RedirectToAction(nameof(Orders));
        }

        public async Task<IActionResult> Products()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
            return View(products);
        }

        public async Task<IActionResult> Customers()
        {
            var users = _userManager.Users.OrderByDescending(u => u.Id).ToList();

            var orderStats = await _context.Orders
                .GroupBy(o => o.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count(), Total = g.Sum(o => o.TotalPrice) })
                .ToListAsync();

            ViewBag.OrderStats = orderStats.ToDictionary(
                x => x.UserId ?? "",
                x => new { x.Count, x.Total });

            return View(users);
        }
    }
}
