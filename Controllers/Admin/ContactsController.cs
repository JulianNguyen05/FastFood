using FastFood.Models;
using System;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity.Validation; // Cần thêm cái này để bắt lỗi Validation chi tiết

namespace FastFood.Controllers.Admin
{
    public class ContactsController : Controller
    {
        private FastFoodDBEntities2 db = new FastFoodDBEntities2();

        // --- 1. QUẢN LÝ DANH SÁCH ---
        public ActionResult Index()
        {
            if (Session["admin"] == null)
            {
                return RedirectToAction("Index", "Login", new { area = "User" });
            }

            // Sắp xếp ngày tạo giảm dần (Mới nhất lên đầu)
            var list = db.LienHes.OrderByDescending(x => x.NgayTao).ToList();
            return View(list);
        }

        // --- 2. XỬ LÝ TRẠNG THÁI (AJAX) ---
        // Hàm này thường được gọi từ Ajax để đổi trạng thái Đã xem/Chưa xem mà không load lại trang
        [HttpPost]
        public ActionResult ToggleStatus(int id)
        {
            try
            {
                var item = db.LienHes.Find(id);
                if (item == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy ID tin nhắn." });
                }

                // Xử lý Nullable Boolean: Nếu null thì coi là false, sau đó đảo ngược
                bool currentStatus = item.TrangThai.GetValueOrDefault(false);
                item.TrangThai = !currentStatus;

                db.SaveChanges();

                return Json(new { success = true, status = item.TrangThai });
            }
            // Bắt lỗi Validation (VD: Trường bắt buộc bị null, quá ký tự cho phép...)
            catch (DbEntityValidationException ex)
            {
                var errorMessages = ex.EntityValidationErrors
                    .SelectMany(x => x.ValidationErrors)
                    .Select(x => $"Field: {x.PropertyName} - Error: {x.ErrorMessage}");

                return Json(new { success = false, message = "Lỗi dữ liệu: " + string.Join("; ", errorMessages) });
            }
            // Bắt lỗi SQL hoặc Server khác
            catch (Exception ex)
            {
                // Đệ quy lấy InnerException để tìm nguyên nhân gốc
                string msg = ex.Message;
                var inner = ex.InnerException;
                while (inner != null)
                {
                    msg += " --> " + inner.Message;
                    inner = inner.InnerException;
                }
                return Json(new { success = false, message = "Lỗi hệ thống: " + msg });
            }
        }

        // --- 3. XÓA DỮ LIỆU ---
        [HttpPost] // Bắt buộc dùng POST để tránh lỗi bảo mật CSRF
        public ActionResult Delete(int id)
        {
            if (Session["admin"] == null) return RedirectToAction("Index", "Login");

            var item = db.LienHes.Find(id);
            if (item != null)
            {
                db.LienHes.Remove(item);
                db.SaveChanges();
                TempData["SuccessMessage"] = "Đã xóa tin nhắn thành công!";
            }
            else
            {
                TempData["Error"] = "Tin nhắn không tồn tại hoặc đã bị xóa.";
            }

            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}