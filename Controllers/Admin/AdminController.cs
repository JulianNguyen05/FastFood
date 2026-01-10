using FastFood.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace FastFood.Controllers.Admin
{
    public class AdminController : Controller
    {
        private FastFoodDBEntities2 db = new FastFoodDBEntities2();

        // Thêm tham số fromDate, toDate
        public ActionResult Default(string fromDate, string toDate)
        {
            // 1. Kiểm tra đăng nhập Admin
            if (Session["admin"] == null)
            {
                return RedirectToAction("Index", "Login", new { area = "User" });
            }

            // 2. Xử lý ngày tháng (Mặc định: 30 ngày gần nhất nếu không chọn)
            DateTime dtFrom = DateTime.Today.AddDays(-29);
            DateTime dtTo = DateTime.Now;

            if (!string.IsNullOrEmpty(fromDate)) DateTime.TryParse(fromDate, out dtFrom);
            if (!string.IsNullOrEmpty(toDate))
            {
                DateTime.TryParse(toDate, out dtTo);
                // Chỉnh về cuối ngày (23:59:59) để lấy trọn vẹn dữ liệu ngày kết thúc
                dtTo = dtTo.Date.AddDays(1).AddTicks(-1);
            }

            // Lưu lại để hiển thị trên View
            ViewBag.FromDate = dtFrom.ToString("yyyy-MM-dd");
            ViewBag.ToDate = dtTo.ToString("yyyy-MM-dd");
            ViewBag.ShowDateRange = $"{dtFrom:dd/MM/yyyy} - {dtTo:dd/MM/yyyy}";

            // 3. Chuẩn bị các truy vấn cơ bản theo ngày
            // Lọc Đơn hàng theo NgayDatHang
            var queryOrders = db.HoaDons.Where(x => x.NgayDatHang >= dtFrom && x.NgayDatHang <= dtTo);
            // Lọc Khách hàng theo NgayTao
            var queryUsers = db.KhachHangs.Where(x => x.NgayTao >= dtFrom && x.NgayTao <= dtTo);
            // Lọc Phản hồi theo NgayTao
            var queryContacts = db.LienHes.Where(x => x.NgayTao >= dtFrom && x.NgayTao <= dtTo);


            // 4. Tính toán số liệu thống kê (BIẾN ĐỘNG THEO NGÀY)

            // Tổng Doanh Thu (chỉ tính đơn Hoàn tất trong khoảng thời gian chọn)
            ViewBag.SoldAmount = queryOrders
                .Where(x => x.TinhTrang == "Hoàn tất")
                .Sum(x => (decimal?)x.TongTien) ?? 0;

            // Đơn chờ xử lý
            ViewBag.PendingCount = queryOrders.Count(x => x.TinhTrang == "Chờ duyệt");

            // Tổng số đơn hàng
            ViewBag.OrderCount = queryOrders.Count();

            // Đơn hoàn tất
            ViewBag.DeliveredCount = queryOrders.Count(x => x.TinhTrang == "Hoàn tất");

            // Khách hàng đăng ký mới
            ViewBag.UserCount = queryUsers.Count();

            // Phản hồi mới nhận
            ViewBag.ContactCount = queryContacts.Count();


            // 5. Số liệu TĨNH (Toàn hệ thống - Không lọc theo ngày)
            // Vì quản lý thường muốn biết "Hiện tại shop có bao nhiêu món", không phải "tháng này thêm bao nhiêu món"
            ViewBag.CategoryCount = db.LoaiSanPhams.Count();
            ViewBag.ProductCount = db.SanPhams.Count();

            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}