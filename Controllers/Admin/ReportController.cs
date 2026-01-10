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
        // Thêm tham số fromDate, toDate để nhận dữ liệu từ bộ lọc
        public ActionResult Index(string fromDate, string toDate)
        {
            if (Session["admin"] == null)
            {
                return RedirectToAction("Index", "Login", new { area = "User" });
            }

            // Mặc định: Lấy 30 ngày gần nhất nếu không chọn
            DateTime dtFrom = DateTime.Today.AddDays(-29);
            DateTime dtTo = DateTime.Now;

            if (!string.IsNullOrEmpty(fromDate)) DateTime.TryParse(fromDate, out dtFrom);
            if (!string.IsNullOrEmpty(toDate))
            {
                DateTime.TryParse(toDate, out dtTo);
                // Chỉnh về cuối ngày (23:59:59) để lấy trọn vẹn dữ liệu ngày kết thúc
                dtTo = dtTo.Date.AddDays(1).AddTicks(-1);
            }

            // Lưu lại ngày đã chọn để hiển thị lại trên giao diện
            ViewBag.FromDate = dtFrom.ToString("yyyy-MM-dd");
            ViewBag.ToDate = dtTo.ToString("yyyy-MM-dd");

            // --- Thống kê số liệu tổng quan (Cards) theo khoảng thời gian ---
            var queryOrders = db.HoaDons.Where(h => h.NgayDatHang >= dtFrom && h.NgayDatHang <= dtTo);

            // 1. Tổng doanh thu (Chỉ tính đơn hoàn tất trong khoảng thời gian này)
            var totalRevenue = queryOrders
                .Where(h => h.TinhTrang == "Hoàn tất")
                .Sum(h => (decimal?)h.TongTien) ?? 0;

            // 2. Tổng đơn hàng (Tất cả trạng thái)
            var totalOrders = queryOrders.Count();

            // 3. Khách hàng mới (Đăng ký trong khoảng thời gian này)
            var totalUsers = db.KhachHangs
                .Count(u => u.NgayTao >= dtFrom && u.NgayTao <= dtTo);

            // 4. Phản hồi mới
            var totalFeedbacks = db.LienHes
                .Count(c => c.NgayTao >= dtFrom && c.NgayTao <= dtTo);

            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TotalOrders = totalOrders;
            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalFeedbacks = totalFeedbacks;

            return View();
        }

        // --- 2. API DỮ LIỆU BIỂU ĐỒ (JSON) ---

        // API 1: Biểu đồ doanh thu (Có lọc ngày)
        [HttpGet]
        public ActionResult GetRevenueChartData(string fromDate, string toDate)
        {
            DateTime dtFrom = DateTime.Today.AddDays(-29);
            DateTime dtTo = DateTime.Now;

            if (!string.IsNullOrEmpty(fromDate)) DateTime.TryParse(fromDate, out dtFrom);
            if (!string.IsNullOrEmpty(toDate))
            {
                DateTime.TryParse(toDate, out dtTo);
                dtTo = dtTo.Date.AddDays(1).AddTicks(-1);
            }

            // Lấy dữ liệu và nhóm theo ngày
            var rawData = db.HoaDons
                .Where(h => h.TinhTrang == "Hoàn tất" && h.NgayDatHang >= dtFrom && h.NgayDatHang <= dtTo)
                .GroupBy(h => DbFunctions.TruncateTime(h.NgayDatHang))
                .Select(g => new
                {
                    Date = g.Key,
                    Revenue = g.Sum(h => (decimal?)h.TongTien) ?? 0
                })
                .OrderBy(x => x.Date)
                .ToList();

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

        // API 2: Biểu đồ Top 5 sản phẩm bán chạy (Có lọc ngày)
        [HttpGet]
        public ActionResult GetTopProductsChartData(string fromDate, string toDate)
        {
            DateTime dtFrom = DateTime.Today.AddDays(-29);
            DateTime dtTo = DateTime.Now;

            if (!string.IsNullOrEmpty(fromDate)) DateTime.TryParse(fromDate, out dtFrom);
            if (!string.IsNullOrEmpty(toDate))
            {
                DateTime.TryParse(toDate, out dtTo);
                dtTo = dtTo.Date.AddDays(1).AddTicks(-1);
            }

            var topProducts = db.ChiTietHoaDons
                .Where(d => d.HoaDon.TinhTrang == "Hoàn tất" && d.HoaDon.NgayDatHang >= dtFrom && d.HoaDon.NgayDatHang <= dtTo)
                .GroupBy(d => d.SanPham.TenSanPham)
                .Select(g => new
                {
                    ProductName = g.Key,
                    Quantity = g.Sum(x => x.SoLuong)
                })
                .OrderByDescending(x => x.Quantity)
                .Take(5)
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