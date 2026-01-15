-- 1. Tạo Database
CREATE DATABASE FastFoodDB;
GO
USE FastFoodDB;
GO

-- 2. Bảng LOẠI SẢN PHẨM
CREATE TABLE LoaiSanPham (
    MaLoaiSP INT IDENTITY(1,1) PRIMARY KEY,
    TenLoaiSP NVARCHAR(100) NOT NULL,
    HinhAnh VARCHAR(MAX),
    TrangThai BIT DEFAULT 1,
    NgayTao DATETIME DEFAULT GETDATE()
);

-- 3. Bảng SẢN PHẨM
CREATE TABLE SanPham (
    MaSanPham INT IDENTITY(1,1) PRIMARY KEY,
    TenSanPham NVARCHAR(200) NOT NULL,
    MoTa NVARCHAR(MAX),
    GiaTien DECIMAL(18,2) NOT NULL,
    SoLuongTon INT DEFAULT 0,
    HinhAnh VARCHAR(MAX),
    MaLoaiSP INT,
    TrangThai BIT DEFAULT 1,
    NgayTao DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_SanPham_Loai FOREIGN KEY (MaLoaiSP) REFERENCES LoaiSanPham(MaLoaiSP)
);

-- 4. Bảng KHÁCH HÀNG (Users)
CREATE TABLE KhachHang (
    MaKhachHang INT IDENTITY(1,1) PRIMARY KEY,
    HoTen NVARCHAR(100),
    TenDangNhap VARCHAR(50) UNIQUE,
    Email VARCHAR(100),
    SoDienThoai VARCHAR(20),
    DiaChi NVARCHAR(MAX),
    MatKhau VARCHAR(255),
    AnhDaiDien VARCHAR(MAX),
    NgayTao DATETIME DEFAULT GETDATE()
);

-- 5. Bảng NHÂN VIÊN
CREATE TABLE NhanVien (
    MaNhanVien INT IDENTITY(1,1) PRIMARY KEY,
    HoTen NVARCHAR(100) NOT NULL,
    TenDangNhap VARCHAR(50) UNIQUE,
    Email VARCHAR(100) UNIQUE,
    SoDienThoai VARCHAR(20),
    MatKhau VARCHAR(255),
    QuyenSuDung NVARCHAR(50), -- Admin, NV Duyệt, NV Giao hàng
    TrangThai BIT DEFAULT 1
);

-- 6. Bảng HÓA ĐƠN (Orders)
CREATE TABLE HoaDon (
    MaHoaDon INT IDENTITY(1,1) PRIMARY KEY,
    SoHoaDon VARCHAR(100) UNIQUE,
    NgayDatHang DATETIME DEFAULT GETDATE(),
    NgayGiaoHang DATETIME,
    TinhTrang NVARCHAR(50), -- Chờ duyệt, Đang giao, Hoàn tất, Đã hủy
    MaKhachHang INT,
    MaNVDuyet INT,
    MaNVGiao INT,
    TongTien DECIMAL(18,2) DEFAULT 0,
    GhiChuHuy NVARCHAR(MAX),
    DiaChiGiao NVARCHAR(MAX),
    SoDienThoaiGiao VARCHAR(20),
    PhuongThucThanhToan NVARCHAR(50),
    TrangThaiThanhToan BIT DEFAULT 0,
    CONSTRAINT FK_HoaDon_KhachHang FOREIGN KEY (MaKhachHang) REFERENCES KhachHang(MaKhachHang),
    CONSTRAINT FK_HoaDon_NVDuyet FOREIGN KEY (MaNVDuyet) REFERENCES NhanVien(MaNhanVien),
    CONSTRAINT FK_HoaDon_NVGiao FOREIGN KEY (MaNVGiao) REFERENCES NhanVien(MaNhanVien)
);

-- 7. Bảng CHI TIẾT HÓA ĐƠN
CREATE TABLE ChiTietHoaDon (
    MaChiTiet INT IDENTITY(1,1) PRIMARY KEY,
    MaHoaDon INT,
    MaSanPham INT,
    SoLuong INT NOT NULL,
    DonGia DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_CTHD_HoaDon FOREIGN KEY (MaHoaDon) REFERENCES HoaDon(MaHoaDon),
    CONSTRAINT FK_CTHD_SanPham FOREIGN KEY (MaSanPham) REFERENCES SanPham(MaSanPham)
);

-- 8. Bảng LIÊN HỆ (Contact)
CREATE TABLE LienHe (
    MaLienHe INT IDENTITY(1,1) PRIMARY KEY,
    HoTen NVARCHAR(100),
    Email VARCHAR(100),
    ChuDe NVARCHAR(200),
    TrangThai BIT DEFAULT 0, -- 0: Chưa xem, 1: Đã xem
    NoiDung NVARCHAR(MAX),
    NgayTao DATETIME DEFAULT GETDATE()
);

-- 9. Bảng GIỎ HÀNG (Cart)
CREATE TABLE GioHang (
    MaGioHang INT IDENTITY(1,1) PRIMARY KEY,
    MaKhachHang INT NOT NULL,
    MaSanPham INT NOT NULL,
    SoLuong INT DEFAULT 1 CHECK (SoLuong > 0),
    NgayTao DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_GioHang_KhachHang FOREIGN KEY (MaKhachHang) REFERENCES KhachHang(MaKhachHang) ON DELETE CASCADE,
    CONSTRAINT FK_GioHang_SanPham FOREIGN KEY (MaSanPham) REFERENCES SanPham(MaSanPham) ON DELETE CASCADE,
    CONSTRAINT UQ_GioHang_Khach_Sanpham UNIQUE (MaKhachHang, MaSanPham)
);

-- 10. Bảng THANH TOÁN
CREATE TABLE ThanhToan (
    MaThanhToan INT IDENTITY(1,1) PRIMARY KEY,
    MaHoaDon INT,
    NgayThanhToan DATETIME DEFAULT GETDATE(),
    SoTien DECIMAL(18,2),
    PhuongThuc NVARCHAR(50),
    GhiChu NVARCHAR(MAX),
    CONSTRAINT FK_ThanhToan_HoaDon FOREIGN KEY (MaHoaDon) REFERENCES HoaDon(MaHoaDon)
);
GO

-- =============================================================
-- INSERT DỮ LIỆU GIẢ (MOCK DATA)
-- =============================================================
-- === PHẦN 1: XÓA DỮ LIỆU CŨ (Nếu muốn làm sạch trước khi test) ===

-- Lưu ý: Thứ tự xóa quan trọng để tránh lỗi Khóa ngoại (Foreign Key)
USE FastFoodDB;
GO

DELETE FROM ThanhToan;
DELETE FROM ChiTietHoaDon;
DELETE FROM GioHang;
DELETE FROM HoaDon;
DELETE FROM LienHe;
DELETE FROM SanPham;
DELETE FROM LoaiSanPham;
DELETE FROM KhachHang;
DELETE FROM NhanVien;

-- Reset lại ID tự tăng về 1 (Tùy chọn)

DBCC CHECKIDENT ('LoaiSanPham', RESEED, 0);
DBCC CHECKIDENT ('SanPham', RESEED, 0);
DBCC CHECKIDENT ('KhachHang', RESEED, 0);
DBCC CHECKIDENT ('NhanVien', RESEED, 0);
DBCC CHECKIDENT ('HoaDon', RESEED, 0);
DBCC CHECKIDENT ('ChiTietHoaDon', RESEED, 0);
GO

-- 1. Insert LOẠI SẢN PHẨM (10 dòng)
INSERT INTO LoaiSanPham (TenLoaiSP, HinhAnh, TrangThai) VALUES 
(N'Combo Gà Rán', '/Images/Categories/combo-ga.png', 1),
(N'Burger Đặc Biệt', '/Images/Categories/burger.png', 1),
(N'Mỳ Ý Sốt Kem', '/Images/Categories/spaghetti.png', 1),
(N'Pizza Hải Sản', '/Images/Categories/pizza.png', 1),
(N'Thức Uống', '/Images/Categories/drinks.png', 1),
(N'Tráng Miệng', '/Images/Categories/dessert.png', 1),
(N'Món Ăn Nhẹ', '/Images/Categories/snack.png', 1),
(N'Cơm Gà', '/Images/Categories/rice.png', 1),
(N'Combo Gia Đình', '/Images/Categories/family.png', 1),
(N'Khuyến Mãi', '/Images/Categories/promo.png', 1);

-- 2. Insert SẢN PHẨM (15 dòng)
INSERT INTO SanPham (TenSanPham, MoTa, GiaTien, SoLuongTon, HinhAnh, MaLoaiSP, TrangThai) VALUES
(N'Gà Rán Giòn Cay', N'Gà rán tẩm bột chiên giòn vị cay nồng', 35000, 100, '/Images/Products/ga-cay.jpg', 1, 1),
(N'Combo 1 Người', N'1 Gà rán + 1 Khoai tây + 1 Pepsi', 79000, 50, '/Images/Products/combo1.jpg', 1, 1),
(N'Burger Bò Phô Mai', N'Burger bò nướng lửa hồng kẹp phô mai', 55000, 80, '/Images/Products/burger-bo.jpg', 2, 1),
(N'Burger Tôm', N'Burger nhân tôm tươi chiên xù', 60000, 60, '/Images/Products/burger-tom.jpg', 2, 1),
(N'Mỳ Ý Bò Bằm', N'Mỳ Ý sốt cà chua thịt bò bằm truyền thống', 45000, 40, '/Images/Products/my-y.jpg', 3, 1),
(N'Pizza Pepperoni', N'Pizza xúc xích cay kiểu Mỹ', 120000, 30, '/Images/Products/pizza-pep.jpg', 4, 1),
(N'Pizza Phô Mai', N'Pizza 4 loại phô mai hảo hạng', 110000, 25, '/Images/Products/pizza-cheese.jpg', 4, 1),
(N'Pepsi Tươi', N'Nước ngọt có ga ly lớn', 15000, 200, '/Images/Products/pepsi.jpg', 5, 1),
(N'Trà Đào Cam Sả', N'Trà đào thanh mát giải nhiệt', 30000, 100, '/Images/Products/tra-dao.jpg', 5, 1),
(N'Kem Vani', N'Kem tươi vị vani ốc quế', 10000, 150, '/Images/Products/ice-cream.jpg', 6, 1),
(N'Khoai Tây Chiên', N'Khoai tây chiên giòn rắc muối', 25000, 100, '/Images/Products/french-fries.jpg', 7, 1),
(N'Cơm Gà Sốt Đậu', N'Cơm trắng ăn kèm gà sốt đậu Hàn Quốc', 40000, 50, '/Images/Products/com-ga.jpg', 8, 1),
(N'Gà Viên Popcorn', N'Gà viên chiên giòn lắc phô mai', 35000, 80, '/Images/Products/popcorn.jpg', 7, 1),
(N'Bánh Tart Trứng', N'Bánh trứng nướng nóng hổi', 18000, 60, '/Images/Products/tart.jpg', 6, 1),
(N'Combo Big Party', N'6 Gà rán + 3 Burger + 3 Nước', 299000, 20, '/Images/Products/big-party.jpg', 9, 1);

-- 3. Insert KHÁCH HÀNG (10 dòng)
-- Mật khẩu giả định là '123456' (đã mã hóa MD5/BCrypt ví dụ)
INSERT INTO KhachHang (HoTen, TenDangNhap, Email, SoDienThoai, DiaChi, MatKhau) VALUES
(N'Nguyễn Văn A', 'user01', 'nguyenvana@gmail.com', '0901234567', N'123 Lê Lợi, Q1, HCM', '123'),
(N'Trần Thị B', 'user02', 'tranthib@gmail.com', '0901234568', N'456 Nguyễn Huệ, Q1, HCM', '123'),
(N'Lê Văn C', 'user03', 'levanc@gmail.com', '0901234569', N'789 Điện Biên Phủ, BT, HCM', '123'),
(N'Phạm Thị D', 'user04', 'phamthid@gmail.com', '0901234570', N'12 CMT8, Q3, HCM', '123'),
(N'Hoàng Văn E', 'user05', 'hoangvane@gmail.com', '0901234571', N'34 Võ Văn Tần, Q3, HCM', '123'),
(N'Đỗ Thị F', 'user06', 'dothif@gmail.com', '0901234572', N'56 Pasteur, Q1, HCM', '123'),
(N'Ngô Văn G', 'user07', 'ngovang@gmail.com', '0901234573', N'78 Hai Bà Trưng, Q1, HCM', '123'),
(N'Bùi Thị H', 'user08', 'buithih@gmail.com', '0901234574', N'90 Lê Duẩn, Q1, HCM', '123'),
(N'Vũ Văn I', 'user09', 'vuvani@gmail.com', '0901234575', N'11 Nguyễn Thị Minh Khai, Q1', '123'),
(N'Đinh Thị K', 'user10', 'dinhthik@gmail.com', '0901234576', N'22 Lý Tự Trọng, Q1, HCM', '123');

-- 4. Insert NHÂN VIÊN (5 dòng: 1 Admin, 2 Duyệt, 2 Giao)
INSERT INTO NhanVien (HoTen, TenDangNhap, Email, SoDienThoai, MatKhau, QuyenSuDung) VALUES
(N'Nhân Viên Duyệt 1', 'DH-duyet01', 'duyet1@fastfood.com', '0988888881', '123', 'NV Duyệt'),
(N'Nhân Viên Duyệt 2', 'DH-duyet02', 'duyet2@fastfood.com', '0988888882', '123', 'NV Duyệt'),
(N'Shipper Hùng', 'GH-giao01', 'hungshipper@fastfood.com', '0977777771', '123', 'NV Giao hàng'),
(N'Shipper Tuấn', 'GH-giao02', 'tuanshipper@fastfood.com', '0977777772', '123', 'NV Giao hàng');

-- 5. Insert HÓA ĐƠN (10 dòng - Đủ các trạng thái)
INSERT INTO HoaDon (SoHoaDon, NgayDatHang, TinhTrang, MaKhachHang, TongTien, DiaChiGiao, PhuongThucThanhToan) VALUES
('DH20231025001', '2023-10-25 10:00:00', N'Hoàn tất', 1, 150000, N'123 Lê Lợi, Q1', 'COD'),
('DH20231025002', '2023-10-25 11:30:00', N'Hoàn tất', 2, 79000, N'456 Nguyễn Huệ, Q1', 'MaQR'),
('DH20231026001', '2023-10-26 09:15:00', N'Đang giao', 3, 220000, N'789 Điện Biên Phủ', 'COD'),
('DH20231026002', '2023-10-26 12:00:00', N'Chờ duyệt', 4, 55000, N'12 CMT8, Q3', 'COD'),
('DH20231026003', '2023-10-26 12:30:00', N'Đã hủy', 5, 120000, N'34 Võ Văn Tần', 'MaQR'),
('DH20231027001', '2023-10-27 18:00:00', N'Hoàn tất', 1, 300000, N'123 Lê Lợi, Q1', 'MaQR'),
('DH20231027002', '2023-10-27 19:30:00', N'Chờ duyệt', 6, 45000, N'56 Pasteur, Q1', 'COD'),
('DH20231028001', '2023-10-28 08:00:00', N'Đang giao', 7, 85000, N'78 Hai Bà Trưng', 'COD'),
('DH20231028002', '2023-10-28 10:00:00', N'Hoàn tất', 8, 150000, N'90 Lê Duẩn, Q1', 'MaQR'),
('DH20231028003', '2023-10-28 11:00:00', N'Chờ duyệt', 9, 60000, N'11 NTMK, Q1', 'COD');

-- 6. Insert CHI TIẾT HÓA ĐƠN (Tương ứng với các hóa đơn trên)
INSERT INTO ChiTietHoaDon (MaHoaDon, MaSanPham, SoLuong, DonGia) VALUES
(1, 1, 2, 35000), (1, 2, 1, 79000), -- Đơn 1
(2, 2, 1, 79000), -- Đơn 2
(3, 6, 1, 120000), (3, 8, 2, 15000), -- Đơn 3
(4, 3, 1, 55000), -- Đơn 4
(5, 7, 1, 110000), -- Đơn 5
(6, 15, 1, 299000), -- Đơn 6
(7, 5, 1, 45000), -- Đơn 7
(8, 1, 1, 35000), (8, 11, 2, 25000), -- Đơn 8
(9, 6, 1, 120000), (9, 8, 2, 15000), -- Đơn 9
(10, 4, 1, 60000); -- Đơn 10

-- 7. Insert LIÊN HỆ (10 dòng)
INSERT INTO LienHe (HoTen, Email, ChuDe, NoiDung, TrangThai) VALUES
(N'Nguyễn Văn A', 'a@gmail.com', N'Góp ý món ăn', N'Gà rán hôm nay hơi mặn', 1),
(N'Trần Thị B', 'b@gmail.com', N'Khen ngợi', N'Nhân viên giao hàng rất thân thiện', 1),
(N'Lê Văn C', 'c@gmail.com', N'Khiếu nại', N'Giao hàng trễ 30 phút', 0),
(N'Phạm Thị D', 'd@gmail.com', N'Hỏi giá', N'Combo 5 người giá bao nhiêu?', 0),
(N'Hoàng Văn E', 'e@gmail.com', N'Đặt tiệc', N'Tôi muốn đặt 50 suất cho công ty', 0),
(N'User F', 'f@gmail.com', N'Quên mật khẩu', N'Làm sao lấy lại mật khẩu?', 1),
(N'User G', 'g@gmail.com', N'Góp ý Web', N'Website load hơi chậm', 0),
(N'User H', 'h@gmail.com', N'Món mới', N'Khi nào có món gà sốt cay?', 0),
(N'User I', 'i@gmail.com', N'Khuyến mãi', N'Chương trình mua 1 tặng 1 còn không?', 1),
(N'User K', 'k@gmail.com', N'Tuyển dụng', N'Shop có tuyển shipper không?', 0);

-- 8. Insert GIỎ HÀNG (Giả lập khách đang chọn món)
INSERT INTO GioHang (MaKhachHang, MaSanPham, SoLuong) VALUES
(1, 3, 2),
(1, 8, 2),
(2, 15, 1),
(3, 5, 1);

-- 9. Insert THANH TOÁN (Lịch sử thanh toán cho các đơn Hoàn tất)
INSERT INTO ThanhToan (MaHoaDon, SoTien, PhuongThuc, GhiChu) VALUES
(1, 150000, 'COD', N'Đã thu tiền mặt'),
(2, 79000, 'MaQR', N'Chuyển khoản VCB...'),
(6, 300000, 'MaQR', N'Chuyển khoản MOMO...'),
(9, 150000, 'MaQR', N'Chuyển khoản BIDV...');
GO