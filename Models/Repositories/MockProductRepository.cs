namespace TrieuDoanKy_W2.Models.Repositories
{
    using System.Linq;

    public class MockProductRepository : IProductRepository
    {
        private readonly List<Product> _products;

        public MockProductRepository()
        {
            _products = new List<Product>
            {
                new Product { Id = 1, Name = "Xi Măng Hà Tiên PCB40 (50kg)", Price = 95000, Description = "Xi măng Hà Tiên loại PCB40, bao 50kg, dùng cho xây dựng dân dụng và công nghiệp.", CategoryId = 1 },
                new Product { Id = 2, Name = "Gạch Đặc 4 Lỗ 8x8x18cm", Price = 2500, Description = "Gạch đặc 4 lỗ tiêu chuẩn, chịu lực tốt, dùng xây tường chịu lực.", CategoryId = 2 },
                new Product { Id = 3, Name = "Thép Hòa Phát D10 (1 thanh)", Price = 125000, Description = "Thép cuộn Hòa Phát D10, dài 11.7m/cây, chịu lực cao, đúng tiêu chuẩn TCVN.", CategoryId = 3 },
                new Product { Id = 4, Name = "Sơn Dulux Nội Thất 4kg", Price = 380000, Description = "Sơn Dulux Easy Clean kháng khuẩn, bề mặt bóng mịn, dễ lau chùi.", CategoryId = 4 },
                new Product { Id = 5, Name = "Gạch Ốp Lát Đồng Tâm 60x60cm", Price = 185000, Description = "Gạch ceramic Đồng Tâm 60x60, men bóng cao cấp, chống trơn trượt.", CategoryId = 2 },
                new Product { Id = 6, Name = "Cát Vàng Bình Dương (1m³)", Price = 280000, Description = "Cát vàng hạt mịn Bình Dương, ít tạp chất, phù hợp trộn bê tông và tô tường.", CategoryId = 1 },
                new Product { Id = 7, Name = "Ống Nhựa PVC Bình Minh D90 (4m)", Price = 145000, Description = "Ống nhựa PVC Bình Minh loại D90, dài 4m/ống, dùng cho hệ thống thoát nước.", CategoryId = 5 },
                new Product { Id = 8, Name = "Tấm Lợp Tôn Mạ Màu 0.45mm", Price = 95000, Description = "Tôn mạ màu dày 0.45mm, chiều dài theo yêu cầu, chống gỉ sét, cách nhiệt tốt.", CategoryId = 3 },
            };
        }

        public IEnumerable<Product> GetAll() => _products;

        public Product GetById(int id) => _products.FirstOrDefault(p => p.Id == id);

        public void Add(Product product)
        {
            product.Id = _products.Count > 0 ? _products.Max(p => p.Id) + 1 : 1;
            _products.Add(product);
        }

        public void Update(Product product)
        {
            var index = _products.FindIndex(p => p.Id == product.Id);
            if (index != -1)
                _products[index] = product;
        }

        public void Delete(int id)
        {
            var product = _products.FirstOrDefault(p => p.Id == id);
            if (product != null)
                _products.Remove(product);
        }
    }
}
