using FastFood.Models;
using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FastFood.Controllers.Admin
{
    public class UsersController : Controller
    {
        private FastFoodDBEntities2 db = new FastFoodDBEntities2();

        // --- 1. HIỂN THỊ DANH SÁCH (GET) ---
        public ActionResult Index()
        {
            var list = db.KhachHangs.OrderByDescending(x => x.MaKhachHang).ToList();
            return View(list);
        }

        // --- 2. HIỂN THỊ FORM (GET) ---
        public ActionResult Form(int? id)
        {
            // Nếu có ID hợp lệ -> Chế độ Edit
            if (id.HasValue && id > 0)
            {
                var item = db.KhachHangs.Find(id);
                if (item == null) return HttpNotFound();
                return View(item);
            }

            // Ngược lại -> Chế độ Create
            return View(new KhachHang());
        }

        // --- 3. XỬ LÝ LƯU DỮ LIỆU (POST) ---
        [HttpPost]
        [ValidateAntiForgeryToken] // Chống tấn công giả mạo request (CSRF)
        public ActionResult Save(KhachHang model, HttpPostedFileBase uploadHinh, string ConfirmPassword)
        {
            // Validate dữ liệu cơ bản
            if (string.IsNullOrEmpty(model.HoTen) || string.IsNullOrEmpty(model.TenDangNhap))
            {
                ModelState.AddModelError("", "Vui lòng nhập họ tên và tên đăng nhập.");
            }

            if (ModelState.IsValid)
            {
                bool isEdit = model.MaKhachHang > 0;

                // --- TRƯỜNG HỢP 1: CẬP NHẬT (EDIT) ---
                if (isEdit)
                {
                    var existItem = db.KhachHangs.Find(model.MaKhachHang);
                    if (existItem == null) return HttpNotFound();

                    // Cập nhật thông tin chung
                    existItem.HoTen = model.HoTen;
                    existItem.Email = model.Email;
                    existItem.SoDienThoai = model.SoDienThoai;
                    existItem.DiaChi = model.DiaChi;

                    // Chỉ cập nhật mật khẩu nếu người dùng nhập mới (Nếu bỏ trống nghĩa là giữ pass cũ)
                    if (!string.IsNullOrEmpty(model.MatKhau))
                    {
                        existItem.MatKhau = model.MatKhau;
                    }

                    // Xử lý ảnh (Gọi hàm helper bên dưới)
                    string newImage = ProcessUploadImage(uploadHinh);
                    if (newImage != null) existItem.AnhDaiDien = newImage;

                    TempData["SuccessMessage"] = "Cập nhật thành công!";
                }
                // --- TRƯỜNG HỢP 2: THÊM MỚI (CREATE) ---
                else
                {
                    // Kiểm tra trùng tên đăng nhập
                    if (db.KhachHangs.Any(x => x.TenDangNhap == model.TenDangNhap))
                    {
                        ModelState.AddModelError("TenDangNhap", "Tên đăng nhập này đã tồn tại!");
                        return View("Form", model);
                    }

                    // Bắt buộc nhập mật khẩu khi tạo mới
                    if (string.IsNullOrEmpty(model.MatKhau))
                    {
                        ModelState.AddModelError("MatKhau", "Vui lòng nhập mật khẩu.");
                        return View("Form", model);
                    }

                    // Xử lý ảnh
                    string newImage = ProcessUploadImage(uploadHinh);
                    model.AnhDaiDien = newImage ?? "/Images/default-user.png"; // Nếu không up ảnh thì dùng ảnh mặc định

                    model.NgayTao = DateTime.Now;
                    db.KhachHangs.Add(model);
                    TempData["SuccessMessage"] = "Thêm khách hàng mới thành công!";
                }

                db.SaveChanges();
                return RedirectToAction("Index");
            }

            // Nếu Validate lỗi -> Trả về Form để nhập lại
            return View("Form", model);
        }

        // --- 4. XÓA DỮ LIỆU ---
        public ActionResult Delete(int id)
        {
            var item = db.KhachHangs.Find(id);
            if (item != null)
            {
                // Lưu ý: Cần kiểm tra ràng buộc khóa ngoại (Đơn hàng của khách) trước khi xóa
                db.KhachHangs.Remove(item);
                db.SaveChanges();
                TempData["SuccessMessage"] = "Đã xóa khách hàng!";
            }
            return RedirectToAction("Index");
        }

        // --- 5. HÀM HỖ TRỢ (HELPER) ---
        // Tách logic lưu ảnh ra riêng để dùng chung cho cả Create và Edit
        private string ProcessUploadImage(HttpPostedFileBase file)
        {
            if (file != null && file.ContentLength > 0)
            {
                string filename = DateTime.Now.ToString("ddMMyyyy_HHmmss") + "_" + Path.GetFileName(file.FileName);
                string folderPath = Server.MapPath("~/Images/Users/");

                // Kiểm tra và tạo thư mục nếu chưa có (Tránh lỗi DirectoryNotFound)
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                string path = Path.Combine(folderPath, filename);
                file.SaveAs(path);
                return "/Images/Users/" + filename;
            }
            return null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}