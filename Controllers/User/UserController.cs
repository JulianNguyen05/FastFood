using FastFood.Models;
using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FastFood.Controllers.User
{
    public class UserController : Controller
    {
        private FastFoodDBEntities2 db = new FastFoodDBEntities2();

        // --- 1. QUẢN LÝ HỒ SƠ & LỊCH SỬ ---
        public ActionResult Profile()
        {
            if (Session["userId"] == null) return RedirectToAction("Index", "Login");

            int maKhachHang = (int)Session["userId"];
            var user = db.KhachHangs.Find(maKhachHang);

            if (user == null) return RedirectToAction("Index", "Login");

            // Load lịch sử đơn hàng (Mới nhất lên đầu)
            ViewBag.History = db.HoaDons
                .Where(h => h.MaKhachHang == maKhachHang)
                .OrderByDescending(h => h.NgayDatHang)
                .ToList();

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateProfile(string HoTen, string SoDienThoai, string DiaChi,
                                          string TenDangNhap, string Email,
                                          string MatKhauCu, string MatKhauMoi, string XacNhanMatKhau,
                                          HttpPostedFileBase uploadAvatar)
        {
            if (Session["userId"] == null) return RedirectToAction("Index", "Login");

            int maKhachHang = (int)Session["userId"];
            var user = db.KhachHangs.Find(maKhachHang);

            if (user != null)
            {
                // A. KIỂM TRA DỮ LIỆU ĐẦU VÀO (VALIDATION)
                // Quan trọng: Phải check (x.Ma != maKhachHang) để tránh báo lỗi khi user giữ nguyên tên cũ
                bool isUserTaken = db.KhachHangs.Any(x => x.TenDangNhap == TenDangNhap && x.MaKhachHang != maKhachHang);
                if (isUserTaken)
                {
                    TempData["Error"] = "Tên đăng nhập này đã có người khác sử dụng!";
                    return RedirectToAction("Profile");
                }

                bool isEmailTaken = db.KhachHangs.Any(x => x.Email == Email && x.MaKhachHang != maKhachHang);
                if (isEmailTaken)
                {
                    TempData["Error"] = "Email này đã được liên kết với tài khoản khác!";
                    return RedirectToAction("Profile");
                }

                // B. XỬ LÝ ĐỔI MẬT KHẨU (Nếu có nhập mật khẩu mới)
                if (!string.IsNullOrEmpty(MatKhauMoi))
                {
                    if (string.IsNullOrEmpty(MatKhauCu))
                    {
                        TempData["Error"] = "Vui lòng nhập mật khẩu hiện tại để xác thực!";
                        return RedirectToAction("Profile");
                    }

                    if (user.MatKhau != MatKhauCu)
                    {
                        TempData["Error"] = "Mật khẩu hiện tại không chính xác!";
                        return RedirectToAction("Profile");
                    }

                    if (MatKhauMoi != XacNhanMatKhau)
                    {
                        TempData["Error"] = "Mật khẩu xác nhận không khớp!";
                        return RedirectToAction("Profile");
                    }

                    user.MatKhau = MatKhauMoi; // Cập nhật pass mới
                }

                // C. CẬP NHẬT THÔNG TIN CƠ BẢN
                user.HoTen = HoTen;
                user.SoDienThoai = SoDienThoai;
                user.DiaChi = DiaChi;
                user.TenDangNhap = TenDangNhap;
                user.Email = Email;

                // D. XỬ LÝ UPLOAD ẢNH ĐẠI DIỆN
                if (uploadAvatar != null && uploadAvatar.ContentLength > 0)
                {
                    // Dùng GUID để tạo tên file duy nhất, tránh trùng lặp
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(uploadAvatar.FileName);
                    string folderPath = Server.MapPath("~/Images/User");

                    if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                    string path = Path.Combine(folderPath, fileName);
                    uploadAvatar.SaveAs(path);

                    user.AnhDaiDien = fileName;
                    Session["avatar"] = fileName; // Cập nhật Session để Header đổi ảnh ngay lập tức
                }

                db.SaveChanges();

                // Cập nhật lại session Username phòng trường hợp user đổi tên đăng nhập
                Session["username"] = user.TenDangNhap;
                TempData["SuccessMessage"] = "Cập nhật hồ sơ thành công!";
            }

            return RedirectToAction("Profile");
        }

        // --- 2. CHI TIẾT ĐƠN HÀNG (INVOICE) ---
        public ActionResult Invoice(int id)
        {
            if (Session["userId"] == null) return RedirectToAction("Index", "Login");

            var hoadon = db.HoaDons.Find(id);

            // Bảo mật: Không tìm thấy HOẶC Đơn hàng này không phải của User đang đăng nhập
            if (hoadon == null || hoadon.MaKhachHang != (int)Session["userId"])
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xem đơn hàng này.";
                return RedirectToAction("Profile");
            }

            return View(hoadon);
        }

        // --- 3. AJAX API ---
        [HttpPost]
        public JsonResult CheckUsernameAvailability(string username)
        {
            if (Session["userId"] == null) return Json(new { status = "error" });

            int currentId = (int)Session["userId"];
            // Kiểm tra trùng lặp (trừ chính user hiện tại)
            bool exists = db.KhachHangs.Any(x => x.TenDangNhap == username && x.MaKhachHang != currentId);

            return Json(new { exists = exists });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}