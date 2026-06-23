using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TrieuDoanKy_W2.Data;
using TrieuDoanKy_W2.Models;
using TrieuDoanKy_W2.Models.ViewModels;
using TrieuDoanKy_W2.Services;

namespace TrieuDoanKy_W2.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;

        public ProductController(ApplicationDbContext context,
                                 UserManager<ApplicationUser> userManager,
                                 IEmailService emailService,
                                 IConfiguration config)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _config = config;
        }

        // GET: Product/Index
        public async Task<IActionResult> Index(string? search, int? categoryId, string? sort, decimal? minPrice, decimal? maxPrice, int page = 1)
        {
            const int pageSize = 12;
            var query = _context.Products.Include(p => p.Category).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Name.Contains(search) || (p.Description != null && p.Description.Contains(search)));
                ViewBag.SearchTerm = search;
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
                ViewBag.SelectedCategoryId = categoryId.Value;
            }

            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);

            // Sort
            query = sort switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "name_asc" => query.OrderBy(p => p.Name),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };
            ViewBag.CurrentSort = sort ?? "newest";
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;

            // Load categories for filter sidebar
            var categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.FilterCategories = categories;
            ViewBag.Categories = new SelectList(categories, "Id", "Name");

            // Avg ratings map
            var ratings = await _context.ProductReviews
                .GroupBy(r => r.ProductId)
                .Select(g => new { ProductId = g.Key, Avg = g.Average(r => (double)r.Rating), Count = g.Count() })
                .ToListAsync();
            
            var ratingsDict = new Dictionary<int, (double Avg, int Count)>();
            foreach (var r in ratings)
            {
                ratingsDict[r.ProductId] = (r.Avg, r.Count);
            }
            ViewBag.Ratings = ratingsDict;

            // Pagination
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            var products = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return View(products);
        }

        // GET: Product/Display/5
        public async Task<IActionResult> Display(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return NotFound();

            // Related products (same category, exclude current)
            var related = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.CategoryId == product.CategoryId && p.Id != id)
                .OrderByDescending(p => p.CreatedAt)
                .Take(4)
                .ToListAsync();
            ViewBag.RelatedProducts = related;

            // Reviews
            var reviews = await _context.ProductReviews
                .Include(r => r.User)
                .Where(r => r.ProductId == id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
            ViewBag.Reviews = reviews;
            ViewBag.AvgRating = reviews.Any() ? reviews.Average(r => (double)r.Rating) : 0.0;
            ViewBag.ReviewCount = reviews.Count;

            // Check if current user can review (bought the product)
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = _userManager.GetUserId(User);
                ViewBag.CanReview = await _context.OrderDetails
                    .AnyAsync(od => od.ProductId == id && od.Order!.UserId == userId && od.Order.Status == "Hoàn thành");
                ViewBag.AlreadyReviewed = await _context.ProductReviews
                    .AnyAsync(r => r.ProductId == id && r.UserId == userId);
                // Wishlist state
                ViewBag.IsWishlisted = await _context.Wishlists
                    .AnyAsync(w => w.ProductId == id && w.UserId == userId);
            }

            return View(product);
        }

        // GET: Product/Add
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Add()
        {
            await LoadCategoriesAsync();
            return View();
        }

        // POST: Product/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Add(Product product, IFormFile? imageUrl, List<IFormFile>? imageUrls)
        {
            if (ModelState.IsValid)
            {
                if (imageUrl != null && imageUrl.Length > 0)
                    product.ImageUrl = await SaveImageAsync(imageUrl);

                if (imageUrls != null && imageUrls.Any())
                {
                    var urls = new List<string>();
                    foreach (var file in imageUrls)
                        if (file.Length > 0)
                            urls.Add(await SaveImageAsync(file));
                    product.ImageUrls = System.Text.Json.JsonSerializer.Serialize(urls);
                }

                product.CreatedAt = DateTime.Now;
                _context.Products.Add(product);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã thêm sản phẩm \"{product.Name}\" thành công!";
                return RedirectToAction(nameof(Index));
            }

            await LoadCategoriesAsync();
            return View(product);
        }

        // GET: Product/Update/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            await LoadCategoriesAsync();
            return View(product);
        }

        // POST: Product/Update/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, Product product, IFormFile? imageUrl, List<IFormFile>? imageUrls)
        {
            if (id != product.Id) return BadRequest();

            if (ModelState.IsValid)
            {
                var existing = await _context.Products.FindAsync(id);
                if (existing == null) return NotFound();

                existing.Name        = product.Name;
                existing.Price       = product.Price;
                existing.Description = product.Description;
                existing.Stock       = product.Stock;
                existing.Unit        = product.Unit;
                existing.CategoryId  = product.CategoryId;
                existing.UpdatedAt   = DateTime.Now;

                if (imageUrl != null && imageUrl.Length > 0)
                    existing.ImageUrl = await SaveImageAsync(imageUrl);

                if (imageUrls != null && imageUrls.Any())
                {
                    var urls = new List<string>();
                    foreach (var file in imageUrls)
                        if (file.Length > 0)
                            urls.Add(await SaveImageAsync(file));
                    existing.ImageUrls = System.Text.Json.JsonSerializer.Serialize(urls);
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã cập nhật sản phẩm \"{existing.Name}\" thành công!";
                return RedirectToAction(nameof(Index));
            }

            await LoadCategoriesAsync();
            return View(product);
        }

        // GET: Product/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return NotFound();
            return View(product);
        }


        // POST: Product/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã xóa sản phẩm \"{product.Name}\" thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        // ────── ĐẶT HÀNG ──────

        // GET: Product/Order/5
        [Authorize]
        public async Task<IActionResult> Order(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            if (product.Stock <= 0)
            {
                TempData["Error"] = "Sản phẩm này hiện đã hết hàng!";
                return RedirectToAction(nameof(Display), new { id });
            }

            var user = await _userManager.GetUserAsync(User);
            var model = new OrderViewModel
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Price = product.Price,
                ImageUrl = product.ImageUrl,
                Unit = product.Unit,
                Stock = product.Stock,
                ReceiverName = user?.FullName ?? "",
                PhoneNumber = user?.PhoneNumber ?? "",
                AddressLine = user?.Address ?? "",
                Province = "TP. Hồ Chí Minh"
            };
            return View(model);
        }

        // POST: Product/Order
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Order(OrderViewModel model)
        {
            // Refill product info for view re-render on error
            var product = await _context.Products.FindAsync(model.ProductId);
            if (product == null) return NotFound();

            model.ProductName = product.Name;
            model.Price = product.Price;
            model.ImageUrl = product.ImageUrl;
            model.Unit = product.Unit;
            model.Stock = product.Stock;

            if (!ModelState.IsValid)
                return View(model);

            // Kiểm tra tồn kho
            if (product.Stock < model.Quantity)
            {
                ModelState.AddModelError("Quantity", $"Số lượng vượt quá tồn kho! Còn {product.Stock} {product.Unit} trong kho.");
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            // Tạo đơn hàng
            var order = new Order
            {
                UserId = user!.Id,
                OrderDate = DateTime.Now,
                TotalPrice = product.Price * model.Quantity,
                Status = "Chờ xác nhận",
                ShippingAddress = model.ShippingAddress,
                PhoneNumber = model.PhoneNumber,
                PaymentMethod = model.PaymentMethod
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Thêm chi tiết đơn hàng
            var detail = new OrderDetail
            {
                OrderId = order.Id,
                ProductId = product.Id,
                Quantity = model.Quantity,
                Price = product.Price
            };
            _context.OrderDetails.Add(detail);

            // Trừ tồn kho
            product.Stock -= model.Quantity;

            await _context.SaveChangesAsync();

            // Gửi email xác nhận
            if (!string.IsNullOrEmpty(user.Email))
            {
                await _emailService.SendOrderConfirmationAsync(
                    user.Email,
                    user.FullName,
                    order.Id,
                    product.Name,
                    model.Quantity,
                    order.TotalPrice,
                    model.ShippingAddress
                );
            }

            TempData["OrderId"] = order.Id;
            TempData["TotalPrice"] = order.TotalPrice.ToString("N0");
            TempData["ProductName"] = product.Name;
            TempData["PaymentMethod"] = model.PaymentMethod;
            return RedirectToAction(nameof(OrderSuccess), new { id = order.Id });
        }

        // GET: Product/OrderSuccess/5
        [Authorize]
        public IActionResult OrderSuccess(int id)
        {
            ViewBag.OrderId = TempData["OrderId"];
            ViewBag.TotalPrice = TempData["TotalPrice"];
            ViewBag.ProductName = TempData["ProductName"];
            ViewBag.PaymentMethod = TempData["PaymentMethod"];

            // VietQR config
            ViewBag.BankId = _config["BankInfo:BankId"];
            ViewBag.AccountNo = _config["BankInfo:AccountNo"];
            ViewBag.AccountName = _config["BankInfo:AccountName"];
            ViewBag.BankName = _config["BankInfo:BankName"];
            return View();
        }

        // ────── GIỎ HÀNG ──────

        [Authorize]
        public async Task<IActionResult> Cart()
        {
            var user = await _userManager.GetUserAsync(User);
            var items = await _context.ShoppingCartItems
                .Where(i => i.ApplicationUserId == user!.Id)
                .Include(i => i.Product)
                .ToListAsync();

            var model = new CartViewModel { Items = items };
            return View(model);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            var existing = await _context.ShoppingCartItems
                .FirstOrDefaultAsync(i => i.ApplicationUserId == user!.Id && i.ProductId == productId);

            int newQty = existing != null ? existing.Quantity + quantity : quantity;

            // Kiểm tra tồn kho
            if (newQty > product.Stock)
            {
                TempData["Error"] = $"Chỉ còn {product.Stock} sản phẩm trong kho!";
                return RedirectToAction(nameof(Display), new { id = productId });
            }

            if (existing != null)
                existing.Quantity = newQty;
            else
                _context.ShoppingCartItems.Add(new ShoppingCartItem
                {
                    ApplicationUserId = user!.Id,
                    ProductId = productId,
                    Quantity = newQty
                });

            await _context.SaveChangesAsync();
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var cartItems = await _context.ShoppingCartItems
                    .Where(i => i.ApplicationUserId == user!.Id)
                    .ToListAsync();
                var itemCount = cartItems.Sum(i => i.Quantity);
                return Json(new { success = true, itemCount = itemCount, message = "Đã thêm vào giỏ hàng!" });
            }

            TempData["Success"] = "Đã thêm vào giỏ hàng!";
            return RedirectToAction(nameof(Display), new { id = productId });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> RemoveFromCart(int id)
        {
            var item = await _context.ShoppingCartItems.FindAsync(id);
            if (item != null)
            {
                _context.ShoppingCartItems.Remove(item);
                await _context.SaveChangesAsync();
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var userId = _userManager.GetUserId(User)!;
                var cartItems = await _context.ShoppingCartItems
                    .Include(i => i.Product)
                    .Where(i => i.ApplicationUserId == userId)
                    .ToListAsync();
                var newTotal = cartItems.Sum(i => (i.Product?.Price ?? 0) * i.Quantity);
                return Json(new { success = true, newTotal = newTotal.ToString("N0"), itemCount = cartItems.Count });
            }
            return RedirectToAction(nameof(Cart));
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UpdateCartQuantity(int id, int quantity)
        {
            var item = await _context.ShoppingCartItems
                .Include(i => i.Product)
                .FirstOrDefaultAsync(i => i.Id == id);

            string? errorMsg = null;
            if (item != null)
            {
                if (quantity > 0)
                {
                    if (item.Product != null && quantity > item.Product.Stock)
                    {
                        errorMsg = $"Chỉ còn {item.Product.Stock} sản phẩm trong kho!";
                        quantity = item.Product.Stock;
                    }
                    item.Quantity = quantity;
                }
                else
                {
                    _context.ShoppingCartItems.Remove(item);
                }
                await _context.SaveChangesAsync();
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var userId = _userManager.GetUserId(User)!;
                var cartItems = await _context.ShoppingCartItems
                    .Include(i => i.Product)
                    .Where(i => i.ApplicationUserId == userId)
                    .ToListAsync();
                var newTotal = cartItems.Sum(i => (i.Product?.Price ?? 0) * i.Quantity);
                var lineTotal = item != null && quantity > 0 ? ((item.Product?.Price ?? 0) * quantity).ToString("N0") : "0";
                return Json(new { success = true, newTotal = newTotal.ToString("N0"), lineTotal, error = errorMsg, itemCount = cartItems.Count });
            }

            if (errorMsg != null) TempData["Error"] = errorMsg;
            return RedirectToAction(nameof(Cart));
        }

        // ────── CHECKOUT TỪ GIỎ HÀNG ──────

        [Authorize]
        public async Task<IActionResult> Checkout()
        {
            var user = await _userManager.GetUserAsync(User);
            var items = await _context.ShoppingCartItems
                .Where(i => i.ApplicationUserId == user!.Id)
                .Include(i => i.Product)
                .ToListAsync();

            if (!items.Any()) return RedirectToAction(nameof(Cart));

            var total = items.Sum(i => (i.Product?.Price ?? 0) * i.Quantity);

            var model = new OrderViewModel
            {
                ProductId = 0, // 0 means multi-item cart checkout
                ProductName = "Đơn hàng từ giỏ hàng (" + items.Count + " loại SP)",
                Price = total,
                Quantity = 1,
                ReceiverName = user?.FullName ?? "",
                PhoneNumber = user?.PhoneNumber ?? "",
                AddressLine = user?.Address ?? "",
                Province = "TP. Hồ Chí Minh"
            };

            return View("Order", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Checkout(OrderViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            var items = await _context.ShoppingCartItems
                .Where(i => i.ApplicationUserId == user!.Id)
                .Include(i => i.Product)
                .ToListAsync();

            if (!items.Any()) return RedirectToAction(nameof(Cart));

            if (!ModelState.IsValid)
            {
                // Re-calculate price for view if validation fails
                model.Price = items.Sum(i => (i.Product?.Price ?? 0) * i.Quantity);
                model.ProductName = "Đơn hàng từ giỏ hàng (" + items.Count + " loại SP)";
                model.Quantity = 1;
                return View("Order", model);
            }

            // Kiểm tra tồn kho cho tất cả sản phẩm
            foreach (var item in items)
            {
                if (item.Product != null && item.Quantity > item.Product.Stock)
                {
                    TempData["Error"] = $"Sản phẩm \"{item.Product.Name}\" chỉ còn {item.Product.Stock} trong kho. Vui lòng cập nhật giỏ hàng.";
                    return RedirectToAction(nameof(Cart));
                }
            }

            var order = new Order
            {
                UserId = user!.Id,
                OrderDate = DateTime.Now,
                TotalPrice = items.Sum(i => (i.Product?.Price ?? 0) * i.Quantity),
                Status = "Chờ xác nhận",
                ShippingAddress = model.ShippingAddress,
                PhoneNumber = model.PhoneNumber,
                PaymentMethod = model.PaymentMethod
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            foreach (var item in items)
            {
                var detail = new OrderDetail
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = item.Product?.Price ?? 0
                };
                _context.OrderDetails.Add(detail);

                // Trừ tồn kho
                if (item.Product != null)
                    item.Product.Stock -= item.Quantity;
            }

            // Xóa giỏ hàng
            _context.ShoppingCartItems.RemoveRange(items);
            await _context.SaveChangesAsync();

            // Email
            if (!string.IsNullOrEmpty(user.Email))
            {
                var itemsDesc = string.Join(", ", items.Select(i => $"{i.Product?.Name} (x{i.Quantity})"));
                await _emailService.SendOrderConfirmationAsync(
                    user.Email, user.FullName, order.Id, itemsDesc,
                    items.Sum(i => i.Quantity), order.TotalPrice, model.ShippingAddress
                );
            }

            TempData["OrderId"] = order.Id;
            TempData["TotalPrice"] = order.TotalPrice.ToString("N0");
            TempData["ProductName"] = "Đơn hàng gồm " + items.Count + " loại sản phẩm";
            TempData["PaymentMethod"] = model.PaymentMethod;
            return RedirectToAction(nameof(OrderSuccess), new { id = order.Id });
        }

        // ────── HELPERS ──────

        private async Task<string> SaveImageAsync(IFormFile image)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + image.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            using var fileStream = new FileStream(filePath, FileMode.Create);
            await image.CopyToAsync(fileStream);
            return "/images/" + uniqueFileName;
        }

        private async Task LoadCategoriesAsync()
        {
            var categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
        }

        // ────── SEARCH AUTOCOMPLETE ──────

        [HttpGet]
        public async Task<IActionResult> SearchSuggestions(string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                return Json(new List<object>());

            var results = await _context.Products
                .Where(p => p.Name.Contains(q))
                .OrderBy(p => p.Name)
                .Take(10)
                .Select(p => new { p.Id, p.Name, p.ImageUrl, Price = p.Price.ToString("N0") })
                .ToListAsync();

            return Json(results);
        }

        // ────── WISHLIST ──────

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ToggleWishlist(int productId)
        {
            var userId = _userManager.GetUserId(User)!;
            var existing = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

            bool liked;
            if (existing != null)
            {
                _context.Wishlists.Remove(existing);
                liked = false;
            }
            else
            {
                _context.Wishlists.Add(new Wishlist { UserId = userId, ProductId = productId, CreatedAt = DateTime.Now });
                liked = true;
            }
            await _context.SaveChangesAsync();
            return Json(new { liked });
        }

        // ────── PRODUCT REVIEWS ──────

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> SubmitReview(int productId, int rating, string? comment)
        {
            var userId = _userManager.GetUserId(User)!;

            // Kiểm tra đã mua chưa
            var hasBought = await _context.OrderDetails
                .AnyAsync(od => od.ProductId == productId && od.Order!.UserId == userId && od.Order.Status == "Hoàn thành");

            if (!hasBought)
            {
                TempData["Error"] = "Bạn cần mua và nhận sản phẩm này trước khi đánh giá.";
                return RedirectToAction(nameof(Display), new { id = productId });
            }

            // Kiểm tra đã review chưa
            var alreadyReviewed = await _context.ProductReviews
                .AnyAsync(r => r.ProductId == productId && r.UserId == userId);

            if (alreadyReviewed)
            {
                TempData["Error"] = "Bạn đã đánh giá sản phẩm này rồi.";
                return RedirectToAction(nameof(Display), new { id = productId });
            }

            rating = Math.Clamp(rating, 1, 5);
            _context.ProductReviews.Add(new ProductReview
            {
                UserId = userId,
                ProductId = productId,
                Rating = rating,
                Comment = comment?.Trim(),
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cảm ơn bạn đã đánh giá sản phẩm!";
            return RedirectToAction(nameof(Display), new { id = productId });
        }
    }
}
