using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using FastFood.Models;

namespace FastFood.Controllers.User
{
    public class MenuController : Controller
    {
        private FastFoodDBEntities2 db = new FastFoodDBEntities2();

        // GET: Menu
        public ActionResult Index(string search = "", int? id = null)
        {
            // --- 1. LẤY DỮ LIỆU TỪ DATABASE ---
            // Sử dụng .Include() để Eager Load toàn bộ sản phẩm của danh mục vào RAM.
            // Lưu ý: Code này sẽ load cả sản phẩm ẩn/hiện về bộ nhớ rồi mới lọc ở bước sau.
            var categories = db.LoaiSanPhams
                .Include(c => c.SanPhams)
                .Where(c => c.TrangThai == true)
                .ToList();

            // --- 2. LỌC DỮ LIỆU (IN-MEMORY) ---
            if (!string.IsNullOrEmpty(search))
            {
                // Trường hợp có tìm kiếm: Lọc theo tên + Trạng thái Active
                string searchLower = search.ToLower(); // Tối ưu: Chỉ convert 1 lần

                foreach (var cat in categories)
                {
                    cat.SanPhams = cat.SanPhams
                        .Where(p => p.TenSanPham.ToLower().Contains(searchLower) && p.TrangThai == true)
                        .ToList();
                }
            }
            else
            {
                // Trường hợp mặc định: Chỉ giữ lại sản phẩm Active
                foreach (var cat in categories)
                {
                    cat.SanPhams = cat.SanPhams
                        .Where(p => p.TrangThai == true)
                        .ToList();
                }
            }

            // --- 3. TRUYỀN DỮ LIỆU RA VIEW ---
            ViewBag.SearchKey = search; // Để hiển thị lại trong ô input
            ViewBag.SelectedCat = id;   // Để UI tự cuộn (scroll) tới danh mục được chọn

            return View(categories);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }

        public ActionResult Details(int id)
        {
            var product = db.SanPhams.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }

            // Gợi ý món ăn cùng loại (Optional - để trang chi tiết đỡ trống)
            ViewBag.RelatedProducts = db.SanPhams
                .Where(p => p.MaLoaiSP == product.MaLoaiSP && p.MaSanPham != id && p.TrangThai == true)
                .Take(4)
                .ToList();

            return View(product);
        }
    }
}