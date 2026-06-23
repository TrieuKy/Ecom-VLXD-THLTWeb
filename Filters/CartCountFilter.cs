using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TrieuDoanKy_W2.Data;
using TrieuDoanKy_W2.Models;

namespace TrieuDoanKy_W2.Filters
{
    /// <summary>
    /// Injects ViewBag.CartCount into every view so _Layout.cshtml
    /// does not need to query the DB directly.
    /// </summary>
    public class CartCountFilter : IAsyncActionFilter
    {
        private readonly ApplicationDbContext _context;

        public CartCountFilter(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var result = await next();

            // Inject after action executes so ViewBag is available on the result
            var userId = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                try
                {
                    var count = await _context.ShoppingCartItems
                        .Where(i => i.ApplicationUserId == userId)
                        .SumAsync(i => i.Quantity);

                    if (result.Result is Microsoft.AspNetCore.Mvc.ViewResult vr && vr.ViewData != null)
                        vr.ViewData["CartCount"] = count;
                }
                catch { /* Graceful degradation */ }
            }
        }
    }
}
