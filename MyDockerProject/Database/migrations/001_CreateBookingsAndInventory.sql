-- Migration: Create Bookings and Inventory tables
-- Run this script on your UmbracoDb database

USE [umbracoDb]
GO

-- Create Bookings table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Bookings]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Bookings] (
        [Id] uniqueidentifier NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [BookingReference] nvarchar(50) NOT NULL,
        [ProductId] uniqueidentifier NOT NULL,
        [ProductType] nvarchar(50) NOT NULL,
        [CheckIn] datetime2 NOT NULL,
        [CheckOut] datetime2 NULL,
        [Quantity] int NOT NULL DEFAULT 1,
        [GuestFirstName] nvarchar(100) NOT NULL,
        [GuestLastName] nvarchar(100) NOT NULL,
        [GuestEmail] nvarchar(255) NOT NULL,
        [GuestPhone] nvarchar(50) NULL,
        [TotalPrice] decimal(18,2) NOT NULL,
        [Currency] nvarchar(10) NOT NULL DEFAULT 'GBP',
        [Status] nvarchar(50) NOT NULL DEFAULT 'Confirmed',
        [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] datetime2 NULL,
        [AdditionalData] nvarchar(max) NULL,
        [UserId] uniqueidentifier NULL
    );
    
    CREATE UNIQUE INDEX [IX_Bookings_BookingReference] ON [dbo].[Bookings] ([BookingReference]);
    CREATE INDEX [IX_Bookings_ProductId_CheckIn] ON [dbo].[Bookings] ([ProductId], [CheckIn], [CheckOut]);
    CREATE INDEX [IX_Bookings_GuestEmail] ON [dbo].[Bookings] ([GuestEmail]);
    CREATE INDEX [IX_Bookings_CreatedAt] ON [dbo].[Bookings] ([CreatedAt]);
    CREATE INDEX [IX_Bookings_UserId] ON [dbo].[Bookings] ([UserId]);
END
GO

-- Create Inventory table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Inventory]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Inventory] (
        [Id] uniqueidentifier NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [ProductId] uniqueidentifier NOT NULL,
        [ProductType] nvarchar(50) NOT NULL,
        [Date] datetime2 NOT NULL,
        [TotalQuantity] int NOT NULL,
        [BookedQuantity] int NOT NULL DEFAULT 0,
        [Price] decimal(18,2) NOT NULL,
        [Currency] nvarchar(10) NOT NULL DEFAULT 'GBP',
        [IsAvailable] bit NOT NULL DEFAULT 1,
        [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [UQ_Inventory_ProductId_Date] UNIQUE ([ProductId], [Date])
    );
    
    CREATE INDEX [IX_Inventory_Date] ON [dbo].[Inventory] ([Date]);
    CREATE INDEX [IX_Inventory_ProductId] ON [dbo].[Inventory] ([ProductId]);
    CREATE INDEX [IX_Inventory_ProductId_Date] ON [dbo].[Inventory] ([ProductId], [Date]);
END
GO

PRINT 'Bookings and Inventory tables created successfully!'
GO


