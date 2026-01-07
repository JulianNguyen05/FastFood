using FastFood.Models;
using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FastFood.Controllers.User
{
    public class LoginController : Controller
    {
        private FastFoodDBEntities2 db = new FastFoodDBEntities2();

        // --- 1. TRANG ĐĂNG NHẬP (GET) ---
        [HttpGet]
        public ActionResult Index()
        {
            // Điều hướng người dùng nếu đã đăng nhập từ trước
            if (Session["userId"] != null) return RedirectToAction("Index", "Home");
            if (Session["admin"] != null || Session["staffId"] != null) return RedirectToAction("Default", "Admin");

            return View();
        }

        // --- 2. XỬ LÝ ĐĂNG NHẬP (POST) ---
        [HttpPost]
        public ActionResult Index(string username, string password, string loginMode)
        {
            // Validate dữ liệu đầu vào
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                TempData["Error"] = "Vui lòng nhập tài khoản và mật khẩu!";
                return View();
            }

            string inputUser = username.Trim();

            // --- NHÁNH 1: QUẢN TRỊ VIÊN & NHÂN VIÊN ---
            if (loginMode == "admin")
            {
                // A. Check Super Admin (Hardcode)
                if (inputUser == "Admin" && password == "123")
                {
                    SetLoginSession("Super Admin", null, "Admin");
                    return ReturnLoginSignal(true, "Xin chào Super Admin!", Url.Action("Default", "Admin"));
                }

                // B. Check Nhân viên trong DB
                var nhanVien = db.NhanViens.FirstOrDefault(u => u.TenDangNhap == inputUser && u.MatKhau == password && u.TrangThai == true);
                if (nhanVien != null)
                {
                    SetLoginSession(nhanVien.HoTen, nhanVien.MaNhanVien, nhanVien.QuyenSuDung);

                    // Điều hướng dựa trên quyền hạn
                    string targetUrl = (nhanVien.QuyenSuDung == "NV Duyệt" || nhanVien.QuyenSuDung == "NV Giao hàng")
                                       ? Url.Action("OrderStatus", "Order")
                                       : Url.Action("Default", "Admin");

                    return ReturnLoginSignal(true, $"Xin chào {nhanVien.HoTen}!", targetUrl);
                }

                // Đăng nhập thất bại
                TempData["Error"] = "Tài khoản quản trị không tồn tại hoặc sai mật khẩu!";
            }
            // --- NHÁNH 2: KHÁCH HÀNG ---
            else
            {
                var khachHang = db.KhachHangs.FirstOrDefault(u => u.TenDangNhap == inputUser && u.MatKhau == password);
                if (khachHang != null)
                {
                    // Lưu Session khách hàng
                    Session["userId"] = khachHang.MaKhachHang;
                    Session["username"] = khachHang.TenDangNhap;
                    Session["avatar"] = khachHang.AnhDaiDien;

                    // Tính toán số lượng giỏ hàng để hiện lên Badge
                    var totalQty = db.GioHangs.Where(g => g.MaKhachHang == khachHang.MaKhachHang).Sum(x => (int?)x.SoLuong) ?? 0;
                    Session["cartCount"] = totalQty;

                    return ReturnLoginSignal(true, "Đăng nhập thành công!", Url.Action("Index", "Home"));
                }

                TempData["Error"] = "Tài khoản hoặc mật khẩu không đúng!";
            }

            // Nếu thất bại: Trả lại username để user đỡ phải nhập lại
            ViewBag.Username = inputUser;
            return View();
        }

        // --- 3. ĐĂNG KÝ (POST) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DangKy(KhachHang model, HttpPostedFileBase uploadAnh)
        {
            if (ModelState.IsValid)
            {
                // 1. Kiểm tra trùng lặp
                if (db.KhachHangs.Any(x => x.TenDangNhap == model.TenDangNhap))
                {
                    TempData["Error"] = "Tên đăng nhập đã tồn tại!";
                    return View(model);
                }

                // 2. Xử lý Upload ảnh
                if (uploadAnh != null && uploadAnh.ContentLength > 0)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(uploadAnh.FileName);
                    string folderPath = Server.MapPath("~/Images/User");

                    if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                    uploadAnh.SaveAs(Path.Combine(folderPath, fileName));
                    model.AnhDaiDien = fileName; // Lưu tên file
                }
                else
                {
                    model.AnhDaiDien = "default-user.png";
                }

                // 3. Lưu DB
                model.NgayTao = DateTime.Now;
                db.KhachHangs.Add(model);
                db.SaveChanges();

                // 4. Auto-fill cho trang Login sau khi đăng ký xong
                TempData["RegisteredUser"] = model.TenDangNhap;
                TempData["RegisteredPass"] = model.MatKhau;
                TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";

                return RedirectToAction("Index");
            }

            TempData["Error"] = "Vui lòng kiểm tra lại thông tin!";
            return View(model);
        }

        // --- 4. CÁC ACTION PHỤ ---

        public ActionResult DangKy() => View();

        public ActionResult DangXuat()
        {
            Session.Clear();   // Xóa dữ liệu
            Session.Abandon(); // Hủy session
            return RedirectToAction("Index");
        }

        // --- 5. HELPER METHODS (Hàm hỗ trợ để code gọn hơn) ---

        private void SetLoginSession(string name, int? id, string role)
        {
            Session["admin"] = name; // Dùng chung key "admin" để check quyền đăng nhập hệ thống
            if (id.HasValue)
            {
                Session["staffId"] = id;
                Session["staffName"] = name;
                Session["staffRole"] = role;
            }
        }

        // Cơ chế Login Signal: Trả về View kèm cờ hiệu để Frontend tự redirect (tránh browser cache hoặc popup blocker)
        private ActionResult ReturnLoginSignal(bool success, string msg, string url)
        {
            ViewBag.LoginSuccess = success;
            ViewBag.WelcomeMsg = msg;
            ViewBag.RedirectUrl = url;
            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}