
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = N'bidnest')
BEGIN
    CREATE DATABASE [bidnest];
END
GO

USE [bidnest];
GO

CREATE TABLE dbo.Roles (
    RoleId    INT IDENTITY(1,1) PRIMARY KEY,
    Name      NVARCHAR(50) NOT NULL UNIQUE
);


CREATE TABLE dbo.Users (
    UserId        INT IDENTITY(1,1) PRIMARY KEY,
    Username      NVARCHAR(100) NOT NULL UNIQUE,
    Email         NVARCHAR(255) NULL,
    PasswordHash  NVARCHAR(512) NOT NULL,
    FullName      NVARCHAR(200) NULL,
    IsBlocked     BIT NOT NULL DEFAULT 0,
    RoleId        INT NOT NULL DEFAULT 2, -- e.g., 1=Admin,2=User
    CreatedAt     DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles(RoleId)
);


CREATE TABLE dbo.Categories (
    CategoryId INT IDENTITY(1,1) PRIMARY KEY,
    Name       NVARCHAR(150) NOT NULL,
    ParentId   INT NULL,
    IsActive   BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_Categories_Parent FOREIGN KEY (ParentId) REFERENCES dbo.Categories(CategoryId)
);


CREATE TABLE dbo.Items (
    ItemId        INT IDENTITY(1,1) PRIMARY KEY,
    SellerId      INT NOT NULL,
    Title         NVARCHAR(200) NOT NULL,
    Description   NVARCHAR(MAX) NULL,
    CategoryId    INT NULL,
    MinBid        DECIMAL(18,2) NOT NULL,
    BidIncrement  DECIMAL(18,2) NOT NULL DEFAULT 1.00,
    StartDate     DATETIME2 NOT NULL,
    EndDate       DATETIME2 NOT NULL,
    Status        NVARCHAR(1) NOT NULL DEFAULT 'A', -- A=Active, I=Inactive, C=Closed
    CreatedAt     DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CurrentPrice  DECIMAL(18,2) NULL, -- denormalized for quick read
    CurrentBidId  INT NULL,
    CONSTRAINT FK_Items_Seller FOREIGN KEY (SellerId) REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_Items_Category FOREIGN KEY (CategoryId) REFERENCES dbo.Categories(CategoryId)
);


CREATE TABLE dbo.ItemImages (
    ImageId   INT IDENTITY(1,1) PRIMARY KEY,
    ItemId    INT NOT NULL,
    Url       NVARCHAR(1000) NOT NULL,
    IsPrimary BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_ItemImages_Item FOREIGN KEY (ItemId) REFERENCES dbo.Items(ItemId)
);


CREATE TABLE dbo.ItemDocuments (
    DocId     INT IDENTITY(1,1) PRIMARY KEY,
    ItemId    INT NOT NULL,
    FileName  NVARCHAR(255) NOT NULL,
    Url       NVARCHAR(1000) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_ItemDocs_Item FOREIGN KEY (ItemId) REFERENCES dbo.Items(ItemId)
);


CREATE TABLE dbo.Bids (
    BidId      INT IDENTITY(1,1) PRIMARY KEY,
    ItemId     INT NOT NULL,
    BidderId   INT NOT NULL,
    Amount     DECIMAL(18,2) NOT NULL,
    BidTime    DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    IsWinning  BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_Bids_Item FOREIGN KEY (ItemId) REFERENCES dbo.Items(ItemId),
    CONSTRAINT FK_Bids_Bidder FOREIGN KEY (BidderId) REFERENCES dbo.Users(UserId)
);


CREATE TABLE dbo.Notifications (
    NotificationId INT IDENTITY(1,1) PRIMARY KEY,
    UserId         INT NOT NULL,
    Title          NVARCHAR(200),
    Message        NVARCHAR(MAX),
    IsRead         BIT NOT NULL DEFAULT 0,
    CreatedAt      DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Notifications_User FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId)
);


CREATE TABLE dbo.Ratings (
    RatingId   INT IDENTITY(1,1) PRIMARY KEY,
    RaterId    INT NOT NULL,
    RatedUserId INT NOT NULL,
    Score      INT NOT NULL, 
    Comment    NVARCHAR(1000) NULL,
    CreatedAt  DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Ratings_Rater FOREIGN KEY (RaterId) REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_Ratings_Rated FOREIGN KEY (RatedUserId) REFERENCES dbo.Users(UserId)
);


CREATE TABLE dbo.Watchlist (
    WatchId INT IDENTITY(1,1) PRIMARY KEY,
    UserId  INT NOT NULL,
    ItemId  INT NOT NULL,
    AddedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_Watch UNIQUE (UserId, ItemId),
    CONSTRAINT FK_Watch_User FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_Watch_Item FOREIGN KEY (ItemId) REFERENCES dbo.Items(ItemId)
);


CREATE INDEX IX_Items_CategoryId ON dbo.Items(CategoryId);
CREATE INDEX IX_Bids_ItemId_Amount ON dbo.Bids(ItemId, Amount DESC);
CREATE INDEX IX_Bids_BidderId ON dbo.Bids(BidderId);
GO
