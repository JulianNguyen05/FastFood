using FastFood.Models;
using System;
using System.Web.Mvc;

namespace FastFood.Controllers.User
{
    public class ContactController : Controller
    {
        private FastFoodDBEntities2 db = new FastFoodDBEntities2();

        // --- 1. HIỂN THỊ FORM (GET) ---
        [HttpGet]
        public ActionResult Index()
        {
            // UX (Trải nghiệm người dùng): Tự động điền tên/email nếu khách đã đăng nhập
            // Giúp khách hàng đỡ phải gõ lại thông tin của chính mình
            if (Session["userId"] != null)
            {
                int userId = (int)Session["userId"];
                var user = db.KhachHangs.Find(userId);
                if (user != null)
                {
                    ViewBag.AutoName = user.HoTen;
                    ViewBag.AutoEmail = user.Email;
                }
            }
            return View();
        }

        // --- 2. XỬ LÝ GỬI TIN (POST) ---
        [HttpPost]
        [ValidateAntiForgeryToken] // Quan trọng: Bảo mật chống tấn công giả mạo request (CSRF)
        public ActionResult Send(LienHe model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    model.NgayTao = DateTime.Now;

                    db.LienHes.Add(model);
                    db.SaveChanges();

                    TempData["SuccessMessage"] = "Cảm ơn bạn đã liên hệ! Chúng tôi sẽ phản hồi sớm nhất.";

                    // Kỹ thuật PRG (Post-Redirect-Get):
                    // Sau khi xử lý POST thành công, LUÔN LUÔN dùng Redirect để chuyển hướng.
                    // Điều này giúp tránh việc user bấm F5 (Refresh) làm gửi lại form lần nữa (Duplicate data).
                    return RedirectToAction("Index");
                }
                catch (Exception)
                {
                    TempData["Error"] = "Có lỗi hệ thống, vui lòng thử lại sau.";
                }
            }
            else
            {
                TempData["Error"] = "Vui lòng nhập đầy đủ thông tin!";
            }

            // Nếu có lỗi (Validate hoặc Exception), trả về View cũ kèm dữ liệu model
            // để người dùng sửa lại mà không bị mất những gì đã nhập.
            return View("Index", model);
        }

        // Clean Up: Giải phóng kết nối Database khi xong việc
        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}