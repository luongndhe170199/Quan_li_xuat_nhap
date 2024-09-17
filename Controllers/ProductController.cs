using Grocery_store.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
namespace Grocery_store.Controllers
{
    public class ProductController : Controller
    {

        private readonly GroceryStoreContext _context; // Sử dụng context để kết nối với database

        public ProductController(GroceryStoreContext context)
        {
            _context = context;
        }
        public IActionResult Index(string searchTerm)
        {
            // Lấy danh sách sản phẩm từ database
            var products = _context.Products.Include(p => p.UnitOfMeasure).OrderBy(p=>p.Name).AsQueryable();

            // Nếu có từ khóa tìm kiếm, lọc theo tên sản phẩm
            if (!string.IsNullOrEmpty(searchTerm))
            {
                products = products.Where(p => p.Name.Contains(searchTerm));
            }

            return View(products.ToList());
        }

        // Hiển thị form thêm sản phẩm
        public IActionResult Create()
        {
            // Lấy danh sách đơn vị đo từ database để hiển thị trong dropdown
            ViewBag.UnitOfMeasureList = _context.UnitOfMeasures.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Product model)
        {
            // Kiểm tra xem người dùng có tải ảnh không
            if (model.ImageFile != null)
            {
                // Định nghĩa đường dẫn để lưu ảnh
                var fileName = Path.GetFileNameWithoutExtension(model.ImageFile.FileName);
                var extension = Path.GetExtension(model.ImageFile.FileName);
                fileName = fileName + DateTime.Now.ToString("yymmssfff") + extension;
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);

                // Lưu file vào thư mục "wwwroot/images"
                using (var stream = new FileStream(path, FileMode.Create))
                {
                    model.ImageFile.CopyTo(stream);
                }

                // Lưu đường dẫn ảnh vào database
                model.ImageUrl = "/images/" + fileName;
            }

            // Kiểm tra xem sản phẩm đã tồn tại chưa
            var existingProduct = _context.Products
                .FirstOrDefault(p => p.Name.ToLower() == model.Name.ToLower() && p.UnitOfMeasureId == model.UnitOfMeasureId);

            if (existingProduct != null)
            {
                // Nếu sản phẩm đã tồn tại, tăng số lượng và cập nhật giá
                existingProduct.Quantity += model.Quantity;
                existingProduct.ImportPrice = model.ImportPrice;
                existingProduct.RetailPrice = model.RetailPrice;
                existingProduct.WholesalePrice = model.WholesalePrice;
                existingProduct.ImageUrl = model.ImageUrl; // Cập nhật ảnh nếu có

                _context.SaveChanges();
            }
            else
            {
                // Thêm sản phẩm mới vào database
                _context.Products.Add(model);
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        // Hiển thị form chỉnh sửa sản phẩm
        public IActionResult Edit(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null)
            {
                return NotFound();
            }

            // Lấy danh sách đơn vị đo để hiển thị trong dropdown
            ViewBag.UnitOfMeasureList = _context.UnitOfMeasures.ToList();
            return View(product);
        }

        // Xử lý cập nhật sản phẩm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Product model)
        {
            var product = _context.Products.Find(model.Id);
            if (product == null)
            {
                return NotFound();
            }

            product.Name = model.Name;
            product.Quantity = model.Quantity;
            product.ImportPrice = model.ImportPrice;
            product.RetailPrice = model.RetailPrice;
            product.WholesalePrice = model.WholesalePrice;
            product.UnitOfMeasureId = model.UnitOfMeasureId;

            // Xử lý cập nhật ảnh nếu có ảnh mới
            if (model.ImageFile != null)
            {
                var fileName = Path.GetFileNameWithoutExtension(model.ImageFile.FileName);
                var extension = Path.GetExtension(model.ImageFile.FileName);
                fileName = fileName + DateTime.Now.ToString("yymmssfff") + extension;
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    model.ImageFile.CopyTo(stream);
                }

                product.ImageUrl = "/images/" + fileName;
            }

            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var product = _context.Products.Include(p => p.OrderDetails).FirstOrDefault(p => p.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            // Xóa các dòng liên quan đến sản phẩm trong bảng OrderDetail
            var orderDetails = _context.OrderDetails.Where(od => od.ProductId == id).ToList();
            _context.OrderDetails.RemoveRange(orderDetails);

            // Xóa sản phẩm
            _context.Products.Remove(product);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }



    }
}
