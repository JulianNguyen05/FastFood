using FastFood.Models;
using System;
using System.IO;
using System.Linq;
using System.Data.Entity;
using System.Web;
using System.Web.Mvc;

namespace FastFood.Controllers.Admin
{
    public class ProductController : Controller
    {
        private FastFoodDBEntities2 db = new FastFoodDBEntities2();

        // --- 1. NHÓM HIỂN THỊ (GET) ---

        public ActionResult Index()
        {
            // Kỹ thuật Eager Loading (.Include): 
            // Load luôn bảng LoaiSanPham để lấy tên loại, tránh lỗi query N+1 khi ra View.
            var list = db.SanPhams.Include(s => s.LoaiSanPham)
                                  .OrderByDescending(x => x.MaSanPham)
                                  .ToList();
            return View(list);
        }

        // Dùng chung Form cho cả Thêm mới và Cập nhật
        public ActionResult Form(int? id)
        {
            // Kiểm tra: Nếu có ID hợp lệ -> Chế độ Edit
            if (id.HasValue && id > 0)
            {
                var item = db.SanPhams.Find(id);
                if (item == null) return HttpNotFound();

                LoadCategoryDropdown(item.MaLoaiSP); // Helper tạo ViewBag
                return View(item);
            }

            // Ngược lại -> Chế độ Create (Khởi tạo giá trị mặc định)
            LoadCategoryDropdown();
            var model = new SanPham { TrangThai = true, SoLuongTon = 0, GiaTien = 0 };
            return View(model);
        }

        // --- 2. NHÓM XỬ LÝ DỮ LIỆU (POST) ---

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(SanPham model, HttpPostedFileBase uploadHinh)
        {
            if (ModelState.IsValid)
            {
                // Xử lý lưu ảnh (logic tách ra hàm riêng ở dưới)
                model.HinhAnh = SaveUploadedImage(uploadHinh) ?? "/Images/default-food.png";
                model.NgayTao = DateTime.Now;

                db.SanPhams.Add(model);
                db.SaveChanges();
                TempData["SuccessMessage"] = "Thêm món ăn mới thành công!";
                return RedirectToAction("Index");
            }

            // Quan trọng: Nếu Validate lỗi, phải load lại Dropdown trước khi trả về View
            LoadCategoryDropdown(model.MaLoaiSP);
            return View("Form", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(SanPham model, HttpPostedFileBase uploadHinh)
        {
            if (ModelState.IsValid)
            {
                var existItem = db.SanPhams.Find(model.MaSanPham);
                if (existItem != null)
                {
                    // Cập nhật thông tin
                    existItem.TenSanPham = model.TenSanPham;
                    existItem.MoTa = model.MoTa;
                    existItem.GiaTien = model.GiaTien;
                    existItem.SoLuongTon = model.SoLuongTon;
                    existItem.MaLoaiSP = model.MaLoaiSP;
                    existItem.TrangThai = model.TrangThai;

                    // Chỉ cập nhật ảnh nếu người dùng có chọn ảnh mới
                    string newImage = SaveUploadedImage(uploadHinh);
                    if (newImage != null) existItem.HinhAnh = newImage;

                    db.SaveChanges();
                    TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công!";
                    return RedirectToAction("Index");
                }
            }

            LoadCategoryDropdown(model.MaLoaiSP);
            return View("Form", model);
        }

        public ActionResult Delete(int id)
        {
            var item = db.SanPhams.Find(id);
            if (item != null)
            {
                // Lưu ý: Cần xử lý ràng buộc khóa ngoại (Chi tiết hóa đơn) trước khi xóa thật
                db.SanPhams.Remove(item);
                db.SaveChanges();
                TempData["SuccessMessage"] = "Đã xóa sản phẩm!";
            }
            return RedirectToAction("Index");
        }

        // --- 3. CÁC HÀM HỖ TRỢ (HELPER) ---

        // Hàm lưu ảnh vào Server
        private string SaveUploadedImage(HttpPostedFileBase file)
        {
            if (file != null && file.ContentLength > 0)
            {
                string fileName = DateTime.Now.ToString("ddMMyyyy_HHmmss") + "_" + Path.GetFileName(file.FileName);
                string folderPath = Server.MapPath("~/Images/Products/");

                // Kiểm tra thư mục tồn tại chưa, nếu chưa thì tạo mới (Tránh lỗi DirectoryNotFound)
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                string path = Path.Combine(folderPath, fileName);
                file.SaveAs(path);
                return "/Images/Products/" + fileName;
            }
            return null;
        }

        // Hàm tạo Dropdown danh mục (Tránh lặp code ở Form, Create, Edit)
        private void LoadCategoryDropdown(int? selectedId = null)
        {
            ViewBag.MaLoaiSP = new SelectList(
                db.LoaiSanPhams.Where(x => x.TrangThai == true),
                "MaLoaiSP",
                "TenLoaiSP",
                selectedId
            );
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}