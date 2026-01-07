using FastFood.Models;
using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FastFood.Controllers.Admin
{
    public class CategoryController : Controller
    {
        private FastFoodDBEntities2 db = new FastFoodDBEntities2();

        // 1. Danh sách danh mục
        public ActionResult Index()
        {
            var list = db.LoaiSanPhams
                         .OrderByDescending(x => x.MaLoaiSP)
                         .ToList();
            return View(list);
        }

        // 2. Hiển thị form (Create / Edit dùng chung View)
        public ActionResult Create()
        {
            var model = new LoaiSanPham
            {
                TrangThai = true // Mặc định kích hoạt
            };
            return View("Form", model);
        }

        public ActionResult Edit(int id)
        {
            var item = db.LoaiSanPhams.Find(id);
            if (item == null) return HttpNotFound();
            return View("Form", item);
        }

        // 3. Xử lý dữ liệu (POST)

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(LoaiSanPham model, HttpPostedFileBase uploadHinh)
        {
            if (ModelState.IsValid)
            {
                // Xử lý upload ảnh
                string imagePath = ProcessUploadImage(uploadHinh);
                model.HinhAnh = !string.IsNullOrEmpty(imagePath)
                                ? imagePath
                                : "/Images/default.png";

                model.NgayTao = DateTime.Now;

                db.LoaiSanPhams.Add(model);
                db.SaveChanges();

                TempData["SuccessMessage"] = "Thêm mới thành công!";
                return RedirectToAction("Index");
            }
            return View("Form", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(LoaiSanPham model, HttpPostedFileBase uploadHinh)
        {
            if (ModelState.IsValid)
            {
                var existingItem = db.LoaiSanPhams.Find(model.MaLoaiSP);
                if (existingItem != null)
                {
                    // Cập nhật thông tin chính
                    existingItem.TenLoaiSP = model.TenLoaiSP;
                    existingItem.TrangThai = model.TrangThai;

                    // Chỉ cập nhật ảnh khi có ảnh mới
                    string newImage = ProcessUploadImage(uploadHinh);
                    if (!string.IsNullOrEmpty(newImage))
                    {
                        existingItem.HinhAnh = newImage;
                    }

                    db.SaveChanges();
                    TempData["SuccessMessage"] = "Cập nhật thành công!";
                    return RedirectToAction("Index");
                }
            }
            return View("Form", model);
        }

        // 4. Xóa danh mục
        public ActionResult Delete(int id)
        {
            var item = db.LoaiSanPhams.Find(id);
            if (item != null)
            {
                // Cần kiểm tra ràng buộc khóa ngoại trước khi xóa
                db.LoaiSanPhams.Remove(item);
                db.SaveChanges();
                TempData["SuccessMessage"] = "Đã xóa danh mục!";
            }
            return RedirectToAction("Index");
        }

        // Hàm dùng chung: xử lý upload ảnh
        private string ProcessUploadImage(HttpPostedFileBase file)
        {
            if (file != null && file.ContentLength > 0)
            {
                string fileName = DateTime.Now.ToString("ddMMyyyy_HHmmss")
                                + "_" + Path.GetFileName(file.FileName);

                string path = Path.Combine(
                    Server.MapPath("~/Images/Categories/"),
                    fileName
                );

                file.SaveAs(path);
                return "/Images/Categories/" + fileName;
            }
            return null;
        }

        // Giải phóng DbContext
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
