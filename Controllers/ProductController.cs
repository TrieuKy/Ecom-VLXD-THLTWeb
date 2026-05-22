using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TrieuDoanKy_W2.Models;
using TrieuDoanKy_W2.Models.Repositories;

namespace TrieuDoanKy_W2.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;

        public ProductController(IProductRepository productRepository, ICategoryRepository categoryRepository)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
        }

        // GET: Product/Index
        public IActionResult Index()
        {
            var products = _productRepository.GetAll();
            return View(products);
        }

        // GET: Product/Display/5
        public IActionResult Display(int id)
        {
            var product = _productRepository.GetById(id);
            if (product == null) return NotFound();
            return View(product);
        }

        // GET: Product/Add
        public IActionResult Add()
        {
            LoadCategories();
            return View();
        }

        // POST: Product/Add  -- FIX: chỉ giữ 1 action Add POST với file upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(Product product, IFormFile imageUrl, List<IFormFile> imageUrls)
        {
            if (ModelState.IsValid)
            {
                // Lưu ảnh chính
                if (imageUrl != null && imageUrl.Length > 0)
                {
                    product.ImageUrl = await SaveImage(imageUrl);
                }

                // Lưu ảnh phụ
                if (imageUrls != null && imageUrls.Count > 0)
                {
                    product.ImageUrls = new List<string>();
                    foreach (var file in imageUrls)
                    {
                        if (file.Length > 0)
                            product.ImageUrls.Add(await SaveImage(file));
                    }
                }

                _productRepository.Add(product);
                TempData["Success"] = $"Đã thêm sản phẩm \"{product.Name}\" thành công!";
                return RedirectToAction("Index");
            }

            LoadCategories();
            return View(product);
        }

        // GET: Product/Update/5
        public IActionResult Update(int id)
        {
            var product = _productRepository.GetById(id);
            if (product == null) return NotFound();
            LoadCategories();
            return View(product);
        }

        // POST: Product/Update  -- FIX: thêm [HttpPost]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Update(Product product)
        {
            if (ModelState.IsValid)
            {
                _productRepository.Update(product);
                TempData["Success"] = $"Đã cập nhật sản phẩm \"{product.Name}\" thành công!";
                return RedirectToAction("Index");
            }

            LoadCategories();
            return View(product);
        }

        // GET: Product/Delete/5
        public IActionResult Delete(int id)
        {
            var product = _productRepository.GetById(id);
            if (product == null) return NotFound();
            return View(product);
        }

        // POST: Product/Delete  -- FIX: dùng asp-action="Delete" trực tiếp
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var product = _productRepository.GetById(id);
            if (product != null)
            {
                _productRepository.Delete(id);
                TempData["Success"] = $"Đã xóa sản phẩm \"{product.Name}\" thành công!";
            }
            return RedirectToAction("Index");
        }

        // Helper: Lưu ảnh vào wwwroot/images
        private async Task<string> SaveImage(IFormFile image)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + image.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(fileStream);
            }

            return "/images/" + uniqueFileName;
        }

        // Helper: Load danh mục vào ViewBag
        private void LoadCategories()
        {
            var categories = _categoryRepository.GetAllCategories();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
        }
    }
}
