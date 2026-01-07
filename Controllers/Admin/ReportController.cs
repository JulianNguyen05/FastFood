using FastFood.Models;
using System;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity; // Quan trọng: Cần để dùng DbFunctions.TruncateTime

namespace FastFood.Controllers.Admin
{
    public class ReportController : Controller
    {
        private FastFoodDBEntities2 db = new FastFoodDBEntities2();

        // --- 1. VIEW BÁO CÁO (HTML) ---
        public ActionResult Index()
        {
            if (Session["admin"] == null)
            {
                return RedirectToAction("Index", "Login", new { area = "User" });
            }

            // --- Thống kê số liệu tổng quan (Cards) ---

            // Doanh thu: Cần ép kiểu (decimal?) và dùng (?? 0) để xử lý trường hợp null (chưa có đơn nào)
            var totalRevenue = db.HoaDons
                .Where(h => h.TinhTrang == "Hoàn tất")
                .Sum(h => (decimal?)h.TongTien) ?? 0;

            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TotalOrders = db.HoaDons.Count();
            ViewBag.TotalUsers = db.KhachHangs.Count();
            ViewBag.TotalFeedbacks = db.LienHes.Count();

            return View();
        }

        // --- 2. API DỮ LIỆU BIỂU ĐỒ (JSON) ---

        // API 1: Biểu đồ doanh thu 7 ngày qua
        [HttpGet]
        public ActionResult GetRevenueChartData()
        {
            var sevenDaysAgo = DateTime.Today.AddDays(-6);

            // BƯỚC 1: Truy vấn SQL (Lấy dữ liệu thô)
            var rawData = db.HoaDons
                .Where(h => h.TinhTrang == "Hoàn tất" && h.NgayDatHang >= sevenDaysAgo)
                // DbFunctions.TruncateTime: Giúp SQL so sánh chỉ ngày tháng, bỏ qua giờ phút giây
                .GroupBy(h => DbFunctions.TruncateTime(h.NgayDatHang))
                .Select(g => new
                {
                    Date = g.Key,
                    Revenue = g.Sum(h => (decimal?)h.TongTien) ?? 0
                })
                .OrderBy(x => x.Date) // Sắp xếp theo ngày tăng dần ngay từ SQL
                .ToList(); // Thực thi câu lệnh SQL tại đây

            // BƯỚC 2: Xử lý bộ nhớ (Format dữ liệu cho ChartJS)
            // Phải tách ra bước 2 vì SQL không hiểu hàm .ToString("dd/MM")
            var chartData = rawData.Select(x => new
            {
                Date = x.Date.Value.ToString("dd/MM"),
                Revenue = x.Revenue
            }).ToList();

            return Json(new
            {
                labels = chartData.Select(x => x.Date),
                values = chartData.Select(x => x.Revenue)
            }, JsonRequestBehavior.AllowGet);
        }

        // API 2: Biểu đồ Top 5 sản phẩm bán chạy
        [HttpGet]
        public ActionResult GetTopProductsChartData()
        {
            var topProducts = db.ChiTietHoaDons
                .Where(d => d.HoaDon.TinhTrang == "Hoàn tất") // Chỉ tính đơn đã bán thành công
                .GroupBy(d => d.SanPham.TenSanPham)           // Gom nhóm theo tên SP
                .Select(g => new
                {
                    ProductName = g.Key,
                    Quantity = g.Sum(x => x.SoLuong)
                })
                .OrderByDescending(x => x.Quantity)           // Sắp xếp giảm dần
                .Take(5)                                      // Lấy 5 dòng đầu
                .ToList();

            return Json(new
            {
                labels = topProducts.Select(x => x.ProductName),
                values = topProducts.Select(x => x.Quantity)
            }, JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}