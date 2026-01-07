using FastFood.Models;
using System;
using System.Collections.Generic;
using System.Linq; // Cần cho LINQ query
using System.Web;
using System.Web.Mvc;
using System.Data.Entity; // Cần cho .Include()

namespace FastFood.Controllers.User
{
    public class HomeController : Controller
    {
        private FastFoodDBEntities2 db = new FastFoodDBEntities2();

        // GET: Home
        public ActionResult Index()
        {
            // --- 1. LOAD DANH MỤC & SẢN PHẨM ---
            // Dùng .Include để Eager Loading: Load luôn sản phẩm con để tránh query lặp lại ở View
            var categories = db.LoaiSanPhams
                .Include(c => c.SanPhams)
                .Where(x => x.TrangThai == true)
                .ToList();

            // --- 2. LOAD FEEDBACK (KỸ THUẬT LEFT JOIN) ---
            // Mục đích: Lấy avatar từ bảng KhachHang nếu email người gửi feedback trùng với email thành viên.
            // Nếu là khách vãng lai (không có trong bảng KhachHang), vẫn lấy feedback nhưng để avatar null.
            var feedbacks = (from lh in db.LienHes
                             where lh.TrangThai == true // Chỉ lấy tin đã duyệt

                             // Join bảng LienHe với KhachHang qua Email
                             join kh in db.KhachHangs on lh.Email equals kh.Email into joined
                             // DefaultIfEmpty() -> Đây là mấu chốt của LEFT JOIN
                             from kh in joined.DefaultIfEmpty()

                             orderby lh.NgayTao descending
                             select new FeedbackData
                             {
                                 HoTen = lh.HoTen,
                                 NoiDung = lh.NoiDung,
                                 ChuDe = lh.ChuDe,
                                 // Kiểm tra null: Nếu tìm thấy khách hàng thì lấy ảnh, không thì null
                                 AnhDaiDien = (kh != null) ? kh.AnhDaiDien : null
                             })
                             .Take(6) // Chỉ lấy 6 tin mới nhất
                             .ToList();

            ViewBag.Feedbacks = feedbacks;

            return View(categories);
        }

        public ActionResult About()
        {
            return View();
        }

        // Giải phóng bộ nhớ database khi xong request
        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }

    // --- DTO (Data Transfer Object) ---
    // Class phụ để hứng dữ liệu tùy chỉnh từ câu lệnh Select bên trên
    public class FeedbackData
    {
        public string HoTen { get; set; }
        public string NoiDung { get; set; }
        public string ChuDe { get; set; }
        public string AnhDaiDien { get; set; }
    }
}