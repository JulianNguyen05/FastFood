using FastFood.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Mvc;

namespace FastFood.Controllers.Admin
{
    public class StaffController : Controller
    {
        private FastFoodDBEntities2 db = new FastFoodDBEntities2();

        // --- 1. CÁC ACTION HIỂN THỊ (GET) ---

        public ActionResult Index()
        {
            var list = db.NhanViens.OrderByDescending(x => x.MaNhanVien).ToList();
            return View(list);
        }

        public ActionResult Form(int? id)
        {
            // Nếu có ID -> Sửa, ngược lại -> Thêm mới
            if (id.HasValue && id > 0)
            {
                var item = db.NhanViens.Find(id);
                if (item == null) return HttpNotFound();

                // Helper load dropdown, chọn sẵn quyền của user
                ViewBag.RoleList = GetRoleSelectList(item.QuyenSuDung);
                return View(item);
            }
            else
            {
                // Helper load dropdown mặc định
                ViewBag.RoleList = GetRoleSelectList();
                return View(new NhanVien { TrangThai = true });
            }
        }

        // --- 2. CÁC ACTION XỬ LÝ DỮ LIỆU (POST) ---

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Save(NhanVien model, string ConfirmPassword)
        {
            // Load lại dropdown ngay đầu hàm để đảm bảo View không lỗi nếu return sớm
            ViewBag.RoleList = GetRoleSelectList(model.QuyenSuDung);

            // --- LOGIC TẠO TÊN ĐĂNG NHẬP TỰ ĐỘNG ---
            // Quy tắc: [Prefix]-[TênLót][Tên] (VD: DH-HuuTrong)
            if (!string.IsNullOrEmpty(model.HoTen))
            {
                string unSignName = ConvertToUnSign(model.HoTen); // Bỏ dấu tiếng Việt
                string[] words = unSignName.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string namePart = "";

                if (words.Length >= 2)
                    namePart = words[words.Length - 2] + words[words.Length - 1]; // Lấy Tên lót + Tên
                else if (words.Length == 1)
                    namePart = words[0];

                string prefix = (model.QuyenSuDung == "NV Giao hàng") ? "GH-" : "DH-";
                model.TenDangNhap = prefix + namePart;
            }

            // --- KIỂM TRA VALIDATION THỦ CÔNG ---

            // 1. Kiểm tra trùng Tên đăng nhập (Trừ chính nó ra nếu đang Edit)
            if (db.NhanViens.Any(x => x.TenDangNhap == model.TenDangNhap && x.MaNhanVien != model.MaNhanVien))
            {
                ModelState.AddModelError("TenDangNhap", $"Tên đăng nhập '{model.TenDangNhap}' đã tồn tại! Hãy thử đổi tên khác.");
            }

            // 2. Kiểm tra trùng Email
            if (db.NhanViens.Any(x => x.Email == model.Email && x.MaNhanVien != model.MaNhanVien))
            {
                ModelState.AddModelError("Email", "Email này đã được sử dụng.");
            }

            // 3. Kiểm tra Mật khẩu khi tạo mới
            if (model.MaNhanVien == 0 && string.IsNullOrEmpty(model.MatKhau))
            {
                ModelState.AddModelError("MatKhau", "Vui lòng nhập mật khẩu cho nhân viên mới.");
            }

            // --- LƯU DATABASE ---
            if (ModelState.IsValid)
            {
                if (model.MaNhanVien > 0) // Cập nhật
                {
                    var existItem = db.NhanViens.Find(model.MaNhanVien);
                    if (existItem != null)
                    {
                        existItem.HoTen = model.HoTen;
                        existItem.TenDangNhap = model.TenDangNhap; // Cập nhật lại username theo tên mới
                        existItem.Email = model.Email;
                        existItem.SoDienThoai = model.SoDienThoai;
                        existItem.QuyenSuDung = model.QuyenSuDung;
                        existItem.TrangThai = model.TrangThai;

                        // Chỉ cập nhật mật khẩu nếu người dùng nhập mới
                        if (!string.IsNullOrEmpty(model.MatKhau))
                        {
                            existItem.MatKhau = model.MatKhau;
                        }
                    }
                    TempData["SuccessMessage"] = "Cập nhật nhân viên thành công!";
                }
                else // Thêm mới
                {
                    db.NhanViens.Add(model);
                    TempData["SuccessMessage"] = "Thêm nhân viên mới thành công!";
                }

                db.SaveChanges();
                return RedirectToAction("Index");
            }

            // Nếu lỗi, trả về View Form kèm thông báo lỗi
            return View("Form", model);
        }

        public ActionResult Delete(int id)
        {
            var item = db.NhanViens.Find(id);
            if (item != null)
            {
                db.NhanViens.Remove(item);
                db.SaveChanges();
                TempData["SuccessMessage"] = "Đã xóa nhân viên!";
            }
            return RedirectToAction("Index");
        }

        // --- 3. CÁC HÀM HỖ TRỢ (HELPER) ---

        // Hàm chuyển tiếng Việt có dấu -> không dấu (Dùng Regex)
        private string ConvertToUnSign(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            Regex regex = new Regex("\\p{IsCombiningDiacriticalMarks}+");
            string temp = s.Normalize(NormalizationForm.FormD);
            return regex.Replace(temp, String.Empty).Replace('\u0111', 'd').Replace('\u0110', 'D');
        }

        // Hàm tạo Dropdown Quyền (Tránh lặp code)
        private SelectList GetRoleSelectList(string selectedValue = null)
        {
            var roles = new List<SelectListItem>
            {
                new SelectListItem { Value = "NV Duyệt", Text = "Nhân viên duyệt đơn" },
                new SelectListItem { Value = "NV Giao hàng", Text = "Nhân viên giao hàng" }
            };
            return new SelectList(roles, "Value", "Text", selectedValue);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}