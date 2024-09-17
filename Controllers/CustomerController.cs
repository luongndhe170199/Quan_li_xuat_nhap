using Grocery_store.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Grocery_store.Controllers
{
    public class CustomerController : Controller
    {
        private readonly GroceryStoreContext _context;

        public CustomerController(GroceryStoreContext context)
        {
            _context = context;
        }

        // Hiển thị danh sách khách hàng
        public IActionResult Index()
        {
            var customers = _context.Customers.OrderBy(c=>c.Name).ToList();
            return View(customers);
        }

        // Hiển thị form thêm khách hàng
        public IActionResult Create()
        {
            var customer = new Customer();  // Khởi tạo đối tượng mới
            return View(customer);  // Truyền đối tượng vào view
        }


        // Xử lý thêm khách hàng mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Customer model)
        {
            if (ModelState.IsValid)
            {
                _context.Customers.Add(model);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(model);
        }

        // Hiển thị form chỉnh sửa khách hàng
        public IActionResult Edit(int id)
        {
            var customer = _context.Customers.Find(id);
            if (customer == null)
            {
                return NotFound();
            }
            return View(customer);
        }

        // Xử lý cập nhật khách hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Customer model)
        {
            if (ModelState.IsValid)
            {
                _context.Customers.Update(model);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(model);
        }

        // Xử lý xóa khách hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            // Tìm khách hàng theo ID
            var customer = _context.Customers
                .Include(c => c.Orders) // Bao gồm các đơn hàng liên quan
                .ThenInclude(o => o.OrderDetails) // Bao gồm các chi tiết đơn hàng liên quan
                .FirstOrDefault(c => c.Id == id);

            if (customer == null)
            {
                return NotFound();
            }

            // Xóa các chi tiết đơn hàng liên quan trước
            foreach (var order in customer.Orders)
            {
                _context.OrderDetails.RemoveRange(order.OrderDetails);
            }

            // Xóa các đơn hàng liên quan
            _context.Orders.RemoveRange(customer.Orders);

            // Xóa khách hàng
            _context.Customers.Remove(customer);

            // Lưu thay đổi vào cơ sở dữ liệu
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }

    }
}
