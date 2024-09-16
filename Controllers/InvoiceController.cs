using Grocery_store.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Grocery_store.Controllers
{
    public class InvoiceController : Controller
    {
        private readonly GroceryStoreContext _context;

        public InvoiceController(GroceryStoreContext context)
        {
            _context = context;
        }

        // Hiển thị form tạo hóa đơn
        public IActionResult Create()
        {
            var customerList = _context.Customers.ToList();
            var productList = _context.Products.ToList();

            if (!customerList.Any())
            {
                ModelState.AddModelError("", "Danh sách khách hàng trống.");
                return View();
            }

            if (!productList.Any())
            {
                ModelState.AddModelError("", "Danh sách sản phẩm trống.");
                return View();
            }

            ViewBag.CustomerList = customerList;
            ViewBag.ProductList = productList;
            return View();
        }

        // Xử lý tạo hóa đơn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Invoice model, int CustomerId, List<int> productIds, List<int> quantities, List<bool> isWholesales, int discount)
        {
            ViewBag.CustomerList = _context.Customers?.ToList() ?? new List<Customer>();
            ViewBag.ProductList = _context.Products?.ToList() ?? new List<Product>();

            // Kiểm tra khách hàng có tồn tại không
            var customer = _context.Customers.Find(CustomerId);
            if (customer == null)
            {
                ModelState.AddModelError("", "Khách hàng không tồn tại.");
                return View();
            }

            // Kiểm tra thông tin sản phẩm
            var products = _context.Products.Where(p => productIds.Contains(p.Id)).ToList();
            if (products.Count != productIds.Count)
            {
                ModelState.AddModelError("", "Một hoặc nhiều sản phẩm không tồn tại.");
                return View();
            }

            // Khởi tạo hóa đơn mới
            var invoice = new Invoice
            {
                Date = DateTime.Now,
                Discount = discount,
                TotalAmount = 0,
                FinalAmount = 0,
            };

            int totalAmount = 0;

            // Xử lý thêm chi tiết hóa đơn
            for (int i = 0; i < productIds.Count; i++)
            {
                var product = products.FirstOrDefault(p => p.Id == productIds[i]);
                if (product != null)
                {
                    if (product.Quantity < quantities[i])
                    {
                        ModelState.AddModelError("", $"Sản phẩm {product.Name} không đủ hàng.");
                        return View();
                    }

                    // Chọn giá bán sỉ hoặc lẻ
                    int unitPrice = isWholesales[i] ? product.WholesalePrice ?? 0 : product.RetailPrice ?? 0;

                    int productTotal = unitPrice * quantities[i];

                    // Tính tổng tiền
                    totalAmount += productTotal;

                    // Tạo chi tiết hóa đơn
                    var orderDetail = new OrderDetail
                    {
                        ProductId = product.Id,
                        Quantity = quantities[i],
                        UnitPrice = unitPrice,
                        IsWholesale = isWholesales[i],
                        Invoice = invoice
                    };

                    _context.OrderDetails.Add(orderDetail);

                    // Cập nhật số lượng sản phẩm trong kho
                    product.Quantity -= quantities[i];
                }
            }

            // Kiểm tra chiết khấu
            if (discount > totalAmount)
            {
                ModelState.AddModelError("", "Chiết khấu không thể lớn hơn tổng tiền.");
                return View();
            }

            // Cập nhật tổng tiền và số tiền cuối cùng
            invoice.TotalAmount = totalAmount;
            invoice.FinalAmount = totalAmount - discount;

            // Lưu hóa đơn vào database
            try
            {
                _context.Invoices.Add(invoice);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Đã xảy ra lỗi khi lưu hóa đơn: " + ex.Message);
                return View();
            }

            return RedirectToAction("Index", "Invoice");
        }

        // Hiển thị danh sách hóa đơn
        public IActionResult Index()
        {
            // Lấy danh sách hóa đơn từ cơ sở dữ liệu
            var invoices = _context.Invoices
                .Include(i => i.OrderDetails)    // Tải OrderDetails liên quan
                .ThenInclude(od => od.Product)   // Tải Product liên quan từ OrderDetails
                .ToList();

            return View(invoices);
        }

        // Phương thức để tính tổng doanh thu theo thời gian
        private int GetRevenue(DateTime startDate, DateTime endDate)
        {
            var revenue = _context.Invoices
                                  .Where(i => i.Date >= startDate && i.Date <= endDate)
                                  .Sum(i => i.FinalAmount);
            return revenue ?? 0;

        }

        // Phương thức để tính tổng vốn nhập vào (giá vốn)
        private int GetCapitalCost(DateTime startDate, DateTime endDate)
        {
            var capitalCost = _context.OrderDetails
                                      .Where(od => od.Invoice.Date >= startDate && od.Invoice.Date <= endDate)
                                      .Sum(od => od.Quantity * od.Product.ImportPrice);
            return capitalCost?? 0;
        }

        // Tính lãi suất = Doanh thu - Vốn nhập vào
        private int GetProfit(DateTime startDate, DateTime endDate)
        {
            var revenue = GetRevenue(startDate, endDate);
            var capitalCost = GetCapitalCost(startDate, endDate);
            return revenue - capitalCost;
        }

        private int GetTotalInvestedCapital()
        {
            // Tính tổng vốn nhập vào cho tất cả sản phẩm trong kho
            var totalInvestedCapital = _context.Products
                                               .Sum(p => p.Quantity * p.ImportPrice);
            return totalInvestedCapital ?? 0;
        }

        // Action để hiển thị doanh thu theo thời gian (ngày, tuần, tháng, năm)
        public IActionResult Revenue()
        {
            // Ngày hiện tại
            var today = DateTime.Now;

            // Doanh thu hôm nay
            var todayRevenue = GetRevenue(today.Date, today.Date);

            // Doanh thu tuần này
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
            var weekRevenue = GetRevenue(startOfWeek, today);

            // Doanh thu tháng này
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var monthRevenue = GetRevenue(startOfMonth, today);

            // Doanh thu năm nay
            var startOfYear = new DateTime(today.Year, 1, 1);
            var yearRevenue = GetRevenue(startOfYear, today);

            // Tính vốn nhập vào và lãi suất
            var todayCapitalCost = GetCapitalCost(today.Date, today.Date);
            var todayProfit = GetProfit(today.Date, today.Date);

            var weekCapitalCost = GetCapitalCost(startOfWeek, today);
            var weekProfit = GetProfit(startOfWeek, today);

            var monthCapitalCost = GetCapitalCost(startOfMonth, today);
            var monthProfit = GetProfit(startOfMonth, today);

            var yearCapitalCost = GetCapitalCost(startOfYear, today);
            var yearProfit = GetProfit(startOfYear, today);

            // Tính tổng vốn đầu tư vào toàn bộ hệ thống
            var totalInvestedCapital = GetTotalInvestedCapital();

            // Truyền dữ liệu vào ViewBag để hiển thị
            ViewBag.TodayRevenue = todayRevenue;
            ViewBag.WeekRevenue = weekRevenue;
            ViewBag.MonthRevenue = monthRevenue;
            ViewBag.YearRevenue = yearRevenue;

            ViewBag.TodayCapitalCost = todayCapitalCost;
            ViewBag.TodayProfit = todayProfit;

            ViewBag.WeekCapitalCost = weekCapitalCost;
            ViewBag.WeekProfit = weekProfit;

            ViewBag.MonthCapitalCost = monthCapitalCost;
            ViewBag.MonthProfit = monthProfit;

            ViewBag.YearCapitalCost = yearCapitalCost;
            ViewBag.YearProfit = yearProfit;

            // Truyền tổng số vốn đầu tư vào ViewBag
            ViewBag.TotalInvestedCapital = totalInvestedCapital;
            return View();
        }
    }
}
