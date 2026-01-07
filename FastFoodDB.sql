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
    CONSTRAINT FK_SanPham_Loai
        FOREIGN KEY (MaLoaiSP) REFERENCES LoaiSanPham(MaLoaiSP)
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
    QuyenSuDung NVARCHAR(50), -- NV Duyệt, NV Giao hàng
    TrangThai BIT DEFAULT 1
);

-- 6. Bảng GIỎ HÀNG / HÓA ĐƠN (Orders)
CREATE TABLE HoaDon (
    MaHoaDon INT IDENTITY(1,1) PRIMARY KEY,
    SoHoaDon VARCHAR(100) UNIQUE,
    NgayDatHang DATETIME DEFAULT GETDATE(),
    NgayGiaoHang DATETIME,
    TinhTrang NVARCHAR(50), -- Chờ duyệt, Đang giao, Hoàn tất
    MaKhachHang INT,
    MaNVDuyet INT,
    MaNVGiao INT,
	TongTien DECIMAL(18,2) DEFAULT 0,
	GhiChuHuy NVARCHAR(MAX),
	DiaChiGiao NVARCHAR(MAX),
	SoDienThoaiGiao VARCHAR(20),
	PhuongThucThanhToan NVARCHAR(50),
	TrangThaiThanhToan BIT DEFAULT 0,
    CONSTRAINT FK_HoaDon_KhachHang
        FOREIGN KEY (MaKhachHang) REFERENCES KhachHang(MaKhachHang),
    CONSTRAINT FK_HoaDon_NVDuyet
        FOREIGN KEY (MaNVDuyet) REFERENCES NhanVien(MaNhanVien),
    CONSTRAINT FK_HoaDon_NVGiao
        FOREIGN KEY (MaNVGiao) REFERENCES NhanVien(MaNhanVien)
);

-- 7. Bảng CHI TIẾT GIỎ HÀNG
CREATE TABLE ChiTietHoaDon (
    MaChiTiet INT IDENTITY(1,1) PRIMARY KEY,
    MaHoaDon INT,
    MaSanPham INT,
    SoLuong INT NOT NULL,
    DonGia DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_CTHD_HoaDon
        FOREIGN KEY (MaHoaDon) REFERENCES HoaDon(MaHoaDon),
    CONSTRAINT FK_CTHD_SanPham
        FOREIGN KEY (MaSanPham) REFERENCES SanPham(MaSanPham)
);

-- 8. Bảng LIÊN HỆ (Contact)
CREATE TABLE LienHe (
    MaLienHe INT IDENTITY(1,1) PRIMARY KEY,
    HoTen NVARCHAR(100),
    Email VARCHAR(100),
    ChuDe NVARCHAR(200),
	TrangThai BIT DEFAULT 0,
    NoiDung NVARCHAR(MAX),
    NgayTao DATETIME DEFAULT GETDATE()
);

-- 9. Bảng GIỎ HÀNG (Cart)
CREATE TABLE GioHang (
    MaGioHang INT IDENTITY(1,1) PRIMARY KEY,
    MaKhachHang INT NOT NULL, -- Để biết giỏ hàng này của ai
    MaSanPham INT NOT NULL,   -- Để biết khách chọn món gì
    SoLuong INT DEFAULT 1 CHECK (SoLuong > 0), -- Số lượng phải lớn hơn 0
    NgayTao DATETIME DEFAULT GETDATE(), -- Ngày thêm vào giỏ

    -- Tạo khóa ngoại liên kết với bảng KhachHang
    CONSTRAINT FK_GioHang_KhachHang 
        FOREIGN KEY (MaKhachHang) REFERENCES KhachHang(MaKhachHang)
        ON DELETE CASCADE, -- Nếu xóa tài khoản khách, xóa luôn giỏ hàng của họ

    -- Tạo khóa ngoại liên kết với bảng SanPham
    CONSTRAINT FK_GioHang_SanPham 
        FOREIGN KEY (MaSanPham) REFERENCES SanPham(MaSanPham)
        ON DELETE CASCADE, -- Nếu sản phẩm bị xóa khỏi hệ thống, xóa khỏi giỏ hàng

    -- Ràng buộc duy nhất: Một khách hàng chỉ có 1 dòng cho 1 sản phẩm 
    -- (Nếu thêm trùng sản phẩm thì chỉ cần tăng số lượng chứ không thêm dòng mới)
    CONSTRAINT UQ_GioHang_Khach_Sanpham UNIQUE (MaKhachHang, MaSanPham)
);
GO

-- 10. Tạo bảng THANH TOÁN (Lưu lịch sử giao dịch)
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








USE FastFoodDB;
GO

-- === PHẦN 1: XÓA DỮ LIỆU CŨ (Nếu muốn làm sạch trước khi test) ===
-- Lưu ý: Thứ tự xóa quan trọng để tránh lỗi Khóa ngoại (Foreign Key)
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

-- === PHẦN 2: TẠO DỮ LIỆU GIẢ ===

-- 1. Thêm Danh Mục (Categories)
INSERT INTO LoaiSanPham (TenLoaiSP, TrangThai) VALUES 
(N'Burger & Sandwich', 1),
(N'Pizza', 1),
(N'Gà Rán', 1),
(N'Đồ Uống', 1),
(N'Món Ăn Kèm', 1);

-- 2. Thêm Sản Phẩm (Products)
-- Lưu ý: Tạo 1 món sắp hết hàng (SoLuongTon < 10) để test cảnh báo kho
INSERT INTO SanPham (TenSanPham, MoTa, GiaTien, SoLuongTon, MaLoaiSP, TrangThai, HinhAnh) VALUES 
(N'Burger Bò Phô Mai', N'Bò nướng lửa hồng kẹp phô mai tan chảy', 65000, 50, 1, 1, 'burger-bo.jpg'),
(N'Burger Gà Giòn', N'Gà chiên giòn rụm với sốt mayonnaise', 55000, 40, 1, 1, 'burger-ga.jpg'),
(N'Pizza Hải Sản', N'Tôm, mực, thanh cua và phô mai mozzarella', 150000, 20, 2, 1, 'pizza-hai-san.jpg'),
(N'Pizza Xúc Xích', N'Xúc xích pepperoni cay nồng', 130000, 8, 2, 1, 'pizza-pepperoni.jpg'), -- Món này tồn kho ít (8)
(N'Gà Rán Cay (3 miếng)', N'Gà tẩm bột ớt cay nồng', 85000, 100, 3, 1, 'ga-ran.jpg'),
(N'Coca Cola Tươi', N'Ly lớn sảng khoái', 20000, 200, 4, 1, 'coke.jpg'),
(N'Khoai Tây Chiên', N'Khoai tây chiên vàng giòn', 30000, 150, 5, 1, 'fries.jpg');

-- 3. Thêm Khách Hàng (Users)
INSERT INTO KhachHang (HoTen, TenDangNhap, Email, MatKhau, DiaChi, NgayTao) VALUES 
(N'Nguyễn Văn A', 'user1', 'nguyenvana@gmail.com', '123456', N'123 Lê Lợi, TP.HCM', GETDATE()),
(N'Trần Thị B', 'user2', 'tranthib@gmail.com', '123456', N'456 Nguyễn Huệ, TP.HCM', GETDATE()),
(N'Lê Văn C', 'user3', 'levanc@gmail.com', '123456', N'789 Hai Bà Trưng, Hà Nội', GETDATE()-5);

-- 4. Thêm Nhân Viên (Staff/Admin)
INSERT INTO NhanVien (HoTen, TenDangNhap, Email, MatKhau, QuyenSuDung, TrangThai) VALUES 
(N'Nhân viên duyet hàng 1', 'duyethang1', 'duyethang1@fastfood.com', '123', N'NV Duyệt', 1),
(N'Nhân Viên Giao Hàng 1', 'giaohang1', 'giaohang1@fastfood.com', '123', N'NV Giao hàng', 1);

-- 5. Thêm Phản Hồi (Feedbacks)
INSERT INTO LienHe (HoTen, Email, ChuDe, NoiDung, TrangThai, NgayTao) VALUES 
(N'Nguyễn Văn A', 'nguyenvana@gmail.com', N'Khen ngợi', N'Món ăn rất ngon, giao hàng nhanh!', 1, GETDATE()),
(N'Trần Thị B', 'tranthib@gmail.com', N'Góp ý', N'Nên thêm nhiều tương ớt hơn.', 0, GETDATE()-1);

-- 6. Thêm Hóa Đơn (Orders) - QUAN TRỌNG CHO REPORT
-- Chúng ta sẽ tạo đơn hàng rải rác trong 7 ngày qua

-- Đơn 1: Hôm nay (Hoàn tất) -> Sẽ tính vào Doanh thu hôm nay
INSERT INTO HoaDon (SoHoaDon, NgayDatHang, TinhTrang, MaKhachHang, TongTien) VALUES 
('ORD-001', GETDATE(), N'Hoàn tất', 1, 150000);

-- Đơn 2: Hôm nay (Chờ duyệt) -> Sẽ hiện số 1 ở ô "Đơn chờ duyệt"
INSERT INTO HoaDon (SoHoaDon, NgayDatHang, TinhTrang, MaKhachHang, TongTien) VALUES 
('ORD-002', GETDATE(), N'Chờ duyệt', 2, 85000);

-- Đơn 3: Hôm qua (Hoàn tất)
INSERT INTO HoaDon (SoHoaDon, NgayDatHang, TinhTrang, MaKhachHang, TongTien) VALUES 
('ORD-003', GETDATE()-1, N'Hoàn tất', 3, 200000);

-- Đơn 4: Hôm qua (Hoàn tất)
INSERT INTO HoaDon (SoHoaDon, NgayDatHang, TinhTrang, MaKhachHang, TongTien) VALUES 
('ORD-004', GETDATE()-1, N'Hoàn tất', 1, 55000);

-- Đơn 5: 3 Ngày trước (Hoàn tất)
INSERT INTO HoaDon (SoHoaDon, NgayDatHang, TinhTrang, MaKhachHang, TongTien) VALUES 
('ORD-005', GETDATE()-3, N'Hoàn tất', 2, 300000);

-- Đơn 6: 5 Ngày trước (Hoàn tất)
INSERT INTO HoaDon (SoHoaDon, NgayDatHang, TinhTrang, MaKhachHang, TongTien) VALUES 
('ORD-006', GETDATE()-5, N'Hoàn tất', 3, 100000);

-- 7. Thêm Chi Tiết Hóa Đơn (Để test Top Sản Phẩm Bán Chạy)
-- Giả sử: 
-- ID 1: Burger Bò (65k)
-- ID 2: Burger Gà (55k)
-- ID 3: Pizza Hải Sản (150k)
-- ID 5: Gà Rán (85k)

-- Chi tiết cho Đơn 1 (150k) = 1 Pizza Hải Sản
INSERT INTO ChiTietHoaDon (MaHoaDon, MaSanPham, SoLuong, DonGia) VALUES (1, 3, 1, 150000);

-- Chi tiết cho Đơn 2 (85k) = 1 Gà Rán
INSERT INTO ChiTietHoaDon (MaHoaDon, MaSanPham, SoLuong, DonGia) VALUES (2, 5, 1, 85000);

-- Chi tiết cho Đơn 3 (200k) = 1 Pizza Hải Sản + 1 Burger Gà (Gà bán chạy)
INSERT INTO ChiTietHoaDon (MaHoaDon, MaSanPham, SoLuong, DonGia) VALUES 
(3, 3, 1, 150000),
(3, 2, 1, 55000);

-- Chi tiết cho Đơn 4 (55k) = 1 Burger Gà (Gà bán chạy lần 2)
INSERT INTO ChiTietHoaDon (MaHoaDon, MaSanPham, SoLuong, DonGia) VALUES (4, 2, 1, 55000);

-- Chi tiết cho Đơn 5 (300k) = 2 Pizza Hải Sản (Pizza bán chạy nhất về doanh thu)
INSERT INTO ChiTietHoaDon (MaHoaDon, MaSanPham, SoLuong, DonGia) VALUES (5, 3, 2, 150000);

-- Chi tiết cho Đơn 6 (100k) = 5 Coca (Bán chạy nhất về số lượng)
INSERT INTO ChiTietHoaDon (MaHoaDon, MaSanPham, SoLuong, DonGia) VALUES (6, 6, 5, 20000);

GO