using FastFood.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FastFood.Controllers.User
{
    public class CartController : Controller
    {
        private FastFoodDBEntities2 db = new FastFoodDBEntities2();

        // --- 1. XEM GIỎ HÀNG (PAGE) ---
        public ActionResult Index()
        {
            if (Session["userId"] == null) return RedirectToAction("Index", "Login");

            int userId = (int)Session["userId"];

            // Quan trọng: Dùng Include("SanPham") để Eager Loading
            // Giúp lấy luôn giá và tên sản phẩm trong 1 câu query, tránh lỗi NullReference khi ra View
            var cartItems = db.GioHangs
                              .Include("SanPham")
                              .Where(g => g.MaKhachHang == userId)
                              .ToList();

            // Tính tổng tiền bằng LINQ (nhanh và gọn hơn vòng lặp foreach)
            ViewBag.TongTien = cartItems.Sum(x => (x.SoLuong ?? 0) * x.SanPham.GiaTien);

            return View(cartItems);
        }

        // --- 2. THÊM VÀO GIỎ (AJAX) ---
        [HttpPost]
        public ActionResult AddToCart(int id)
        {
            if (Session["userId"] == null)
                return Json(new { success = false, msg = "Bạn cần đăng nhập!", requireLogin = true });

            int userId = (int)Session["userId"];
            var cartItem = db.GioHangs.FirstOrDefault(x => x.MaKhachHang == userId && x.MaSanPham == id);

            if (cartItem != null)
            {
                cartItem.SoLuong++; // Nếu đã có -> Tăng số lượng
            }
            else
            {
                // Nếu chưa có -> Tạo mới
                var newItem = new GioHang
                {
                    MaKhachHang = userId,
                    MaSanPham = id,
                    SoLuong = 1,
                    NgayTao = DateTime.Now
                };
                db.GioHangs.Add(newItem);
            }

            db.SaveChanges();
            UpdateSessionCount(userId); // Cập nhật Badge trên Header

            return Json(new
            {
                success = true,
                msg = "Đã thêm món ngon vào giỏ! 😋",
                totalItems = Session["cartCount"] // Lấy trực tiếp từ Session vừa update
            });
        }

        // --- 3. CẬP NHẬT SỐ LƯỢNG (AJAX) ---
        [HttpPost]
        public ActionResult UpdateQuantity(int id, int quantity)
        {
            if (Session["userId"] == null) return Json(new { success = false });

            int userId = (int)Session["userId"];

            // Cần Include SanPham để lấy được đơn giá tính tiền
            var item = db.GioHangs.Include("SanPham")
                                  .FirstOrDefault(x => x.MaKhachHang == userId && x.MaSanPham == id);

            if (item != null)
            {
                item.SoLuong = quantity;

                // Logic nghiệp vụ: Nếu giảm về 0 hoặc âm thì xóa luôn
                if (item.SoLuong <= 0) db.GioHangs.Remove(item);

                db.SaveChanges();
                UpdateSessionCount(userId);

                // Tính toán lại các con số để trả về cho Client update giao diện ngay lập tức
                decimal itemTotal = (item.SoLuong ?? 0) * item.SanPham.GiaTien;
                decimal grandTotal = GetCartGrandTotal(userId);

                return Json(new
                {
                    success = true,
                    itemTotal = itemTotal.ToString("N0") + " đ",
                    grandTotal = grandTotal.ToString("N0") + " đ"
                });
            }
            return Json(new { success = false });
        }

        // --- 4. XÓA MÓN (AJAX) ---
        [HttpPost]
        public ActionResult Remove(int id)
        {
            if (Session["userId"] == null) return Json(new { success = false });

            int userId = (int)Session["userId"];
            var item = db.GioHangs.FirstOrDefault(x => x.MaKhachHang == userId && x.MaSanPham == id);

            if (item != null)
            {
                db.GioHangs.Remove(item);
                db.SaveChanges();
                UpdateSessionCount(userId);

                decimal grandTotal = GetCartGrandTotal(userId);
                return Json(new { success = true, grandTotal = grandTotal.ToString("N0") + " đ" });
            }
            return Json(new { success = false });
        }

        // --- 5. CÁC HÀM HELPER (PRIVATE) ---

        // Helper 1: Cập nhật Session đếm số lượng (Dùng hiển thị số trên icon giỏ hàng)
        private void UpdateSessionCount(int userId)
        {
            var count = db.GioHangs.Where(x => x.MaKhachHang == userId).Sum(x => x.SoLuong);
            Session["cartCount"] = count ?? 0;
        }

        // Helper 2: Tính tổng tiền cả giỏ hàng (Tránh lặp code ở UpdateQuantity và Remove)
        private decimal GetCartGrandTotal(int userId)
        {
            return db.GioHangs.Where(x => x.MaKhachHang == userId)
                              .Sum(x => (x.SoLuong ?? 0) * x.SanPham.GiaTien);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}