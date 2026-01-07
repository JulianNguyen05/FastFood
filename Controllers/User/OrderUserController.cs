using FastFood.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI; // Cần cho HtmlTextWriter

namespace FastFood.Controllers.User
{
    public class OrderUserController : Controller
    {
        private FastFoodDBEntities2 db = new FastFoodDBEntities2();

        // --- 1. TRANG THANH TOÁN (VIEW) ---
        public ActionResult Checkout()
        {
            if (Session["userId"] == null)
                return RedirectToAction("Index", "Login", new { area = "User" });

            int userId = (int)Session["userId"];

            // Load giỏ hàng (Eager Loading sản phẩm để tính tiền)
            var cartItems = db.GioHangs.Include("SanPham").Where(g => g.MaKhachHang == userId).ToList();

            if (cartItems.Count == 0) return RedirectToAction("Index", "Cart");

            // Tính toán hiển thị
            ViewBag.TongTien = cartItems.Sum(x => (x.SoLuong ?? 0) * x.SanPham.GiaTien);
            ViewBag.CartItems = cartItems;

            // Truyền User object để auto-fill form
            var user = db.KhachHangs.Find(userId);
            return View(user);
        }

        // --- 2. XỬ LÝ ĐẶT HÀNG (POST - TRANSACTION) ---
        [HttpPost]
        public ActionResult Checkout(string DiaChiGiao, string SoDienThoaiGiao, string PhuongThuc, string GhiChu)
        {
            if (Session["userId"] == null) return RedirectToAction("Index", "Login");

            int userId = (int)Session["userId"];

            // Lấy dữ liệu giỏ hàng hiện tại để chuyển thành đơn hàng
            var cartItems = db.GioHangs.Include("SanPham").Where(g => g.MaKhachHang == userId).ToList();
            if (cartItems.Count == 0) return RedirectToAction("Index", "Cart");

            decimal tongTien = cartItems.Sum(x => (x.SoLuong ?? 0) * x.SanPham.GiaTien);

            // BẮT ĐẦU GIAO DỊCH (Quan trọng: Đảm bảo ACID)
            // Nếu có bất kỳ lỗi nào xảy ra trong block này, mọi thay đổi DB sẽ bị hủy bỏ (Rollback)
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // Bước 1: Tạo Hóa đơn (Header)
                    HoaDon order = new HoaDon
                    {
                        MaKhachHang = userId,
                        NgayDatHang = DateTime.Now,
                        SoHoaDon = "DH" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                        TinhTrang = "Chờ duyệt",
                        TongTien = tongTien,
                        DiaChiGiao = DiaChiGiao,
                        SoDienThoaiGiao = SoDienThoaiGiao,
                        PhuongThucThanhToan = PhuongThuc,
                        GhiChuHuy = GhiChu,
                        TrangThaiThanhToan = (PhuongThuc == "MaQR") // Logic giả định
                    };
                    db.HoaDons.Add(order);
                    db.SaveChanges(); // Save lần 1 để lấy MaHoaDon (Identity)

                    // Bước 2: Tạo Chi tiết hóa đơn (Detail)
                    foreach (var item in cartItems)
                    {
                        ChiTietHoaDon detail = new ChiTietHoaDon
                        {
                            MaHoaDon = order.MaHoaDon,
                            MaSanPham = item.MaSanPham,
                            SoLuong = item.SoLuong ?? 1,
                            DonGia = item.SanPham.GiaTien
                        };
                        db.ChiTietHoaDons.Add(detail);
                    }

                    // Bước 3: Lưu lịch sử thanh toán
                    ThanhToan history = new ThanhToan
                    {
                        MaHoaDon = order.MaHoaDon,
                        NgayThanhToan = DateTime.Now,
                        SoTien = tongTien,
                        PhuongThuc = PhuongThuc,
                        GhiChu = "Thanh toán đơn hàng " + order.SoHoaDon
                    };
                    db.ThanhToans.Add(history);

                    // Bước 4: Xóa giỏ hàng sau khi đặt thành công
                    db.GioHangs.RemoveRange(cartItems);

                    db.SaveChanges();      // Save tất cả thay đổi còn lại
                    transaction.Commit();  // CHỐT GIAO DỊCH (Dữ liệu chính thức được lưu)

                    // Reset Session và chuyển hướng
                    Session["cartCount"] = 0;
                    return RedirectToAction("Invoice", "User", new { id = order.MaHoaDon });
                }
                catch (Exception)
                {
                    transaction.Rollback(); // Gặp lỗi -> Hoàn tác mọi thứ như chưa hề đặt hàng
                    return View("Error");   // Nên tạo View Error riêng hoặc return Content("Lỗi...")
                }
            }
        }

        // --- 3. XUẤT HÓA ĐƠN EXCEL ---
        public void ExportToExcel(int id)
        {
            var order = db.HoaDons.Include("ChiTietHoaDons.SanPham")
                                  .Include("KhachHang")
                                  .FirstOrDefault(h => h.MaHoaDon == id);

            if (order != null)
            {
                // Cấu hình Response Header để trình duyệt hiểu đây là file Excel
                Response.ClearContent();
                Response.Buffer = true;
                Response.AddHeader("content-disposition", "attachment; filename=HoaDon-" + order.SoHoaDon + ".xls");
                Response.ContentType = "application/ms-excel";
                Response.Charset = "utf-8"; // Thêm charset để đỡ lỗi font tiếng Việt

                using (StringWriter sw = new StringWriter())
                {
                    using (HtmlTextWriter htw = new HtmlTextWriter(sw))
                    {
                        // Render nội dung HTML Table
                        // Excel đời cũ có thể đọc Table HTML và hiển thị như bảng tính
                        htw.Write("<h3>HÓA ĐƠN BÁN HÀNG</h3>");
                        htw.Write($"<p>Mã đơn: <b>{order.SoHoaDon}</b></p>");
                        htw.Write($"<p>Khách hàng: {order.KhachHang.HoTen}</p>");
                        htw.Write($"<p>Ngày đặt: {order.NgayDatHang:dd/MM/yyyy HH:mm}</p>");

                        htw.Write("<table border='1' style='border-collapse:collapse;'>");
                        htw.Write("<thead><tr style='background-color:#eee;'><th>Sản phẩm</th><th>Số lượng</th><th>Đơn giá</th><th>Thành tiền</th></tr></thead>");
                        htw.Write("<tbody>");

                        foreach (var item in order.ChiTietHoaDons)
                        {
                            htw.Write("<tr>");
                            htw.Write($"<td>{item.SanPham.TenSanPham}</td>");
                            htw.Write($"<td style='text-align:center;'>{item.SoLuong}</td>");
                            htw.Write($"<td style='text-align:right;'>{item.DonGia:N0}</td>");
                            htw.Write($"<td style='text-align:right;'>{(item.SoLuong * item.DonGia):N0}</td>");
                            htw.Write("</tr>");
                        }

                        htw.Write($"<tr><td colspan='3' style='text-align:right;'><b>TỔNG CỘNG</b></td><td style='text-align:right;'><b>{order.TongTien:N0}</b></td></tr>");
                        htw.Write("</tbody></table>");
                    }

                    Response.Output.Write(sw.ToString());
                    Response.Flush();
                    Response.End(); // Kết thúc request để file được tải xuống
                }
            }
        }

        // Clean Up connection
        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}