-- Test Data untuk DBTestAplikasi
-- Jalankan script ini di SQL Server untuk menambahkan test data

USE [DBTestAplikasi]
GO

-- Insert Test Users
INSERT INTO [dbo].[Users] (Username, Password, NamaLengkap, Role, Email, Status)
VALUES 
('admin', 'admin123', 'Administrator', 'Admin', 'admin@test.com', 'Aktif'),
('pimpinan', 'pimpinan123', 'Pimpinan Toko', 'Pimpinan', 'pimpinan@test.com', 'Aktif'),
('kasir1', 'kasir123', 'Kasir Pertama', 'Kasir', 'kasir1@test.com', 'Aktif'),
('kasir2', 'kasir123', 'Kasir Kedua', 'Kasir', 'kasir2@test.com', 'Aktif');

-- Insert Test Products
INSERT INTO [dbo].[Produk] (NamaProduk, Harga, Deskripsi, HargaModal, HargaJual)
VALUES 
('Laptop ASUS', 12000000, 'Laptop Gaming ASUS ROG', 10000000, 12500000),
('Monitor LG 24 inch', 2500000, 'Monitor Full HD LG', 2000000, 2600000),
('Keyboard Mechanical', 800000, 'Keyboard RGB Mechanical', 600000, 850000),
('Mouse Logitech', 500000, 'Mouse Wireless Logitech', 350000, 550000),
('Headset Gaming', 1500000, 'Headset 7.1 Surround', 1000000, 1600000);

-- Insert Test Stock
DECLARE @ProdukId INT;
SET @ProdukId = (SELECT TOP 1 Id FROM [dbo].[Produk] WHERE NamaProduk = 'Laptop ASUS');
INSERT INTO [dbo].[StokProduk] (ProdukId, Jumlah, Keterangan, TanggalMasuk)
VALUES (@ProdukId, 15, 'Stok awal', GETDATE());

SET @ProdukId = (SELECT TOP 1 Id FROM [dbo].[Produk] WHERE NamaProduk = 'Monitor LG 24 inch');
INSERT INTO [dbo].[StokProduk] (ProdukId, Jumlah, Keterangan, TanggalMasuk)
VALUES (@ProdukId, 25, 'Stok awal', GETDATE());

SET @ProdukId = (SELECT TOP 1 Id FROM [dbo].[Produk] WHERE NamaProduk = 'Keyboard Mechanical');
INSERT INTO [dbo].[StokProduk] (ProdukId, Jumlah, Keterangan, TanggalMasuk)
VALUES (@ProdukId, 40, 'Stok awal', GETDATE());

SET @ProdukId = (SELECT TOP 1 Id FROM [dbo].[Produk] WHERE NamaProduk = 'Mouse Logitech');
INSERT INTO [dbo].[StokProduk] (ProdukId, Jumlah, Keterangan, TanggalMasuk)
VALUES (@ProdukId, 50, 'Stok awal', GETDATE());

SET @ProdukId = (SELECT TOP 1 Id FROM [dbo].[Produk] WHERE NamaProduk = 'Headset Gaming');
INSERT INTO [dbo].[StokProduk] (ProdukId, Jumlah, Keterangan, TanggalMasuk)
VALUES (@ProdukId, 20, 'Stok awal', GETDATE());

-- Insert Test Transactions
DECLARE @KasirId INT = (SELECT TOP 1 Id FROM [dbo].[Users] WHERE Role = 'Kasir');
DECLARE @TransaksiId INT;

INSERT INTO [dbo].[Transaksi] (NomorTransaksi, KasirId, TotalHarga, Bayar, Kembalian, TanggalTransaksi)
VALUES ('TRX-' + FORMAT(GETDATE(), 'yyyyMMdd') + '-001', @KasirId, 15500000, 15500000, 0, GETDATE());

SET @TransaksiId = SCOPE_IDENTITY();

-- Insert Transaction Details
INSERT INTO [dbo].[TransaksiDetail] (TransaksiId, ProdukId, NamaProduk, HargaJual, Jumlah, Subtotal)
SELECT 
    @TransaksiId,
    (SELECT TOP 1 Id FROM [dbo].[Produk] WHERE NamaProduk = 'Laptop ASUS'),
    'Laptop ASUS',
    12500000,
    1,
    12500000;

INSERT INTO [dbo].[TransaksiDetail] (TransaksiId, ProdukId, NamaProduk, HargaJual, Jumlah, Subtotal)
SELECT 
    @TransaksiId,
    (SELECT TOP 1 Id FROM [dbo].[Produk] WHERE NamaProduk = 'Mouse Logitech'),
    'Mouse Logitech',
    550000,
    2,
    1100000;

-- Insert another transaction
INSERT INTO [dbo].[Transaksi] (NomorTransaksi, KasirId, TotalHarga, Bayar, Kembalian, TanggalTransaksi)
VALUES ('TRX-' + FORMAT(GETDATE(), 'yyyyMMdd') + '-002', @KasirId, 3200000, 3200000, 0, GETDATE());

SET @TransaksiId = SCOPE_IDENTITY();

INSERT INTO [dbo].[TransaksiDetail] (TransaksiId, ProdukId, NamaProduk, HargaJual, Jumlah, Subtotal)
SELECT 
    @TransaksiId,
    (SELECT TOP 1 Id FROM [dbo].[Produk] WHERE NamaProduk = 'Monitor LG 24 inch'),
    'Monitor LG 24 inch',
    2600000,
    1,
    2600000;

INSERT INTO [dbo].[TransaksiDetail] (TransaksiId, ProdukId, NamaProduk, HargaJual, Jumlah, Subtotal)
SELECT 
    @TransaksiId,
    (SELECT TOP 1 Id FROM [dbo].[Produk] WHERE NamaProduk = 'Keyboard Mechanical'),
    'Keyboard Mechanical',
    850000,
    1,
    600000;

GO

PRINT 'Test data sudah berhasil di-insert!';
PRINT 'Test Login Credentials:';
PRINT '==========================================';
PRINT 'Username: admin       | Password: admin123      | Role: Admin';
PRINT 'Username: pimpinan    | Password: pimpinan123   | Role: Pimpinan';
PRINT 'Username: kasir1      | Password: kasir123      | Role: Kasir';
PRINT '==========================================';
