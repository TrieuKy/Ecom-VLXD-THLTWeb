namespace TrieuDoanKy_W2.Models.Repositories
{
    public class MockCategoryRepository : ICategoryRepository
    {
        private readonly List<Category> _categoryList;

        public MockCategoryRepository()
        {
            _categoryList = new List<Category>
            {
                new Category { Id = 1, Name = "Xi Măng & Vữa" },
                new Category { Id = 2, Name = "Gạch & Đá" },
                new Category { Id = 3, Name = "Thép & Kim Loại" },
                new Category { Id = 4, Name = "Sơn & Chống Thấm" },
                new Category { Id = 5, Name = "Ống Nhựa & Phụ Kiện" },
                new Category { Id = 6, Name = "Thiết Bị Vệ Sinh" },
                new Category { Id = 7, Name = "Điện & Chiếu Sáng" },
            };
        }

        public IEnumerable<Category> GetAllCategories() => _categoryList;
    }
}
