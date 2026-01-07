using FastFood.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace FastFood.Controllers.Admin
{
    public class AdminController : Controller
    {
        private FastFoodDBEntities2 db = new FastFoodDBEntities2();

        public ActionResult Default()
        {
            // 1. Kiểm tra đăng nhập Admin
            if (Session["admin"] == null)
            {
                return RedirectToAction("Index", "Login", new { area = "User" });
            }

            // 2. Thống kê dữ liệu tổng quát
            ViewBag.CategoryCount = db.LoaiSanPhams.Count();
            ViewBag.ProductCount = db.SanPhams.Count();
            ViewBag.UserCount = db.KhachHangs.Count();
            ViewBag.ContactCount = db.LienHes.Count();

            // 3. Thống kê đơn hàng theo trạng thái
            ViewBag.OrderCount = db.HoaDons.Count();
            ViewBag.DeliveredCount = db.HoaDons.Count(x => x.TinhTrang == "Hoàn tất");
            ViewBag.PendingCount = db.HoaDons.Count(x => x.TinhTrang == "Chờ duyệt");

            // 4. Tính tổng doanh thu (tránh lỗi null khi chưa có đơn hàng)
            ViewBag.SoldAmount = db.HoaDons
                .Where(x => x.TinhTrang == "Hoàn tất")
                .Sum(x => (decimal?)x.TongTien) ?? 0;

            return View();
        }

        // Giải phóng tài nguyên DbContext
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
