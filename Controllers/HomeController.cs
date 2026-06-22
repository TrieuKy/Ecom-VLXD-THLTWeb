using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrieuDoanKy_W2.Data;
using TrieuDoanKy_W2.Models;

namespace TrieuDoanKy_W2.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories.Take(6).ToListAsync();
            var products = await _context.Products.Include(p => p.Category).OrderByDescending(p => p.CreatedAt).Take(8).ToListAsync();

            ViewBag.Categories = categories;
            ViewBag.FeaturedProducts = products;
            
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Promotions()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
