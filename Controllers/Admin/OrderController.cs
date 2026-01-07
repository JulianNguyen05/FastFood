using FastFood.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace FastFood.Controllers.Admin
{
    public class OrderController : Controller
    {
        private FastFoodDBEntities2 db = new FastFoodDBEntities2();

        // --- NHÓM 1: XEM DỮ LIỆU (READ) ---

        public ActionResult OrderStatus()
        {
            // Kiểm tra Session: Phải là Admin hoặc Nhân viên mới được vào
            if (Session["staffId"] == null && Session["admin"] == null)
                return RedirectToAction("Index", "Login", new { area = "User" });

            int staffId = Convert.ToInt32(Session["staffId"] ?? 0);
            string role = Session["staffRole"] as string;

            // Fix lỗi logic: Nếu là Admin đăng nhập thì role mặc định là Admin
            if (Session["admin"] != null && Session["staffId"] == null) role = "Admin";

            // Kỹ thuật Eager Loading: Dùng .Include để load luôn dữ liệu bảng quan hệ
            // Giúp tránh lỗi NullReference khi gọi Model.KhachHang.Ten trong View
            var query = db.HoaDons
                .Include(h => h.KhachHang)
                .Include(h => h.NhanVien)  // Load thông tin NV Duyệt
                .Include(h => h.NhanVien1) // Load thông tin NV Giao
                .OrderByDescending(h => h.NgayDatHang)
                .AsQueryable();

            // Phân quyền dữ liệu: Chỉ hiện đơn hàng liên quan đến vai trò
            if (role == "NV Duyệt")
            {
                // Chỉ thấy đơn Chờ duyệt hoặc đơn mình đã duyệt
                query = query.Where(x => x.TinhTrang == "Chờ duyệt" || x.MaNVDuyet == staffId);
            }
            else if (role == "NV Giao hàng")
            {
                // Chỉ thấy đơn Đã duyệt (để nhận giao) hoặc đơn mình đang giao
                query = query.Where(x => x.TinhTrang == "Đã duyệt" || x.MaNVGiao == staffId);
            }
            // Admin: Thấy toàn bộ (không lọt vào if/else trên)

            return View(query.ToList());
        }

        public ActionResult Details(int id)
        {
            var details = db.ChiTietHoaDons
                    .Include(d => d.SanPham)
                    .Include(d => d.HoaDon)
                    .Where(d => d.MaHoaDon == id)
                    .ToList();

            ViewBag.TongTien = details.Sum(x => x.SoLuong * x.DonGia);

            // Trả về PartialView để hiển thị Popup (Modal)
            return PartialView("_OrderDetails", details);
        }

        // --- NHÓM 2: XỬ LÝ TRẠNG THÁI (WRITE - Happy Path) ---
        // Thứ tự: Duyệt -> Đi giao -> Hoàn tất

        [HttpPost]
        public ActionResult ApproveOrder(int id)
        {
            var order = db.HoaDons.Find(id);
            if (order != null && order.TinhTrang == "Chờ duyệt")
            {
                order.TinhTrang = "Đã duyệt";

                // Lưu vết người thực hiện hành động duyệt
                if (Session["staffId"] != null)
                {
                    order.MaNVDuyet = Convert.ToInt32(Session["staffId"]);
                }

                db.SaveChanges();
                TempData["SuccessMessage"] = "Đã duyệt đơn hàng #" + order.SoHoaDon;
            }
            return RedirectToAction("OrderStatus");
        }

        [HttpPost]
        public ActionResult StartShipping(int id)
        {
            var order = db.HoaDons.Find(id);
            // Chỉ cho phép đi giao khi đơn đã được duyệt
            if (order != null && order.TinhTrang == "Đã duyệt")
            {
                order.TinhTrang = "Đang giao";
                order.NgayGiaoHang = DateTime.Now; // Cập nhật thời điểm bắt đầu giao

                // Gán đơn hàng này cho nhân viên đang đăng nhập
                if (Session["staffId"] != null)
                {
                    order.MaNVGiao = Convert.ToInt32(Session["staffId"]);
                }

                db.SaveChanges();
                TempData["SuccessMessage"] = "Bắt đầu giao đơn hàng #" + order.SoHoaDon;
            }
            return RedirectToAction("OrderStatus");
        }

        [HttpPost]
        public ActionResult CompleteOrder(int id)
        {
            var order = db.HoaDons.Find(id);
            int currentStaffId = Convert.ToInt32(Session["staffId"] ?? 0);
            bool isAdmin = Session["admin"] != null;

            // Logic ràng buộc: Chỉ Shipper phụ trách đơn đó (hoặc Admin) mới được bấm Hoàn tất
            bool canComplete = (order.MaNVGiao == currentStaffId) || isAdmin;

            if (order != null && order.TinhTrang == "Đang giao" && canComplete)
            {
                order.TinhTrang = "Hoàn tất";
                order.TrangThaiThanhToan = true; // Xác nhận đã thu tiền
                order.NgayGiaoHang = DateTime.Now; // Cập nhật thời điểm hoàn thành thực tế

                db.SaveChanges();
                TempData["SuccessMessage"] = "Đã hoàn tất đơn hàng #" + order.SoHoaDon;
            }
            return RedirectToAction("OrderStatus");
        }

        // --- NHÓM 3: XỬ LÝ NGOẠI LỆ (Unhappy Path) ---

        [HttpPost]
        public ActionResult RejectOrder(int id, string lyDo)
        {
            var order = db.HoaDons.Find(id);
            bool isAdmin = Session["admin"] != null;

            // Logic hủy: NV chỉ hủy được khi chưa duyệt. Admin được quyền hủy kể cả khi đang giao.
            bool canReject = (order.TinhTrang == "Chờ duyệt") || (isAdmin && order.TinhTrang != "Hoàn tất");

            if (order != null && canReject)
            {
                order.TinhTrang = "Đã hủy";
                order.GhiChuHuy = lyDo;

                if (Session["staffId"] != null)
                {
                    order.MaNVDuyet = Convert.ToInt32(Session["staffId"]);
                }

                db.SaveChanges();
                TempData["SuccessMessage"] = "Đã hủy đơn hàng #" + order.SoHoaDon;
            }
            return RedirectToAction("OrderStatus");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}