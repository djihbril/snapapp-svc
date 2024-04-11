IF OBJECT_ID('dbo.Logins', 'U') IS NOT NULL
BEGIN
    DELETE FROM Logins;
END
GO

IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL
BEGIN
    DELETE FROM Users;
END
GO

DROP TABLE IF EXISTS Logins;
GO

DROP TABLE IF EXISTS Users;
GO

CREATE TABLE Users
(
    [Id] UNIQUEIDENTIFIER PRIMARY KEY,
    [Email] NVARCHAR(40) UNIQUE NOT NULL,
    [Password] VARCHAR(60) NOT NULL,
    [Company] VARCHAR(60) NOT NULL,
    [FirstName] NVARCHAR(40) NOT NULL,
    [LastName] NVARCHAR(40) NOT NULL,
    [Phone] VARCHAR(20) NOT NULL,
    [Role] VARCHAR(10) NOT NULL,
    [IsEmailVerified] BIT DEFAULT 0,
    [Picture] VARCHAR(60),
    [Salt] BINARY(16) NOT NULL,
    [CreatedOn] DATETIME2 DEFAULT GETUTCDATE()
);
GO

CREATE TABLE Logins
(
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [UserId] UNIQUEIDENTIFIER NOT NULL,
    [CryptoKeys] BINARY(1172) NOT NULL,
    [ExpiresOn] DATETIME2 DEFAULT GETUTCDATE(),
    [CreatedOn] DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY(UserId) REFERENCES Users(Id)
);

DROP PROCEDURE IF EXISTS CheckExistingUser;
GO

CREATE PROC CheckExistingUser
(
    @email NVARCHAR(40)
)
AS
    SELECT
        CASE
            WHEN EXISTS (SELECT 1 FROM Users WHERE Email = @email)
            THEN 1 ELSE 0
        END AS UserExists;
GO

DROP PROCEDURE IF EXISTS GetLoginInfo;
GO

CREATE PROC GetLoginInfo
(
    @email NVARCHAR(40)
)
AS
    SELECT
        l.Id, u.Id AS UserId, u.Email AS UserEmail, u.[Password] AS UserPassword, u.Company AS UserCompany, u.FirstName AS UserFirstName, u.LastName AS UserLastName,
        u.Phone AS UserPhone, u.[Role] AS UserRole, u.IsEmailVerified AS IsUserEmailVerified, u.Picture AS UserPicture, u.CreatedOn AS UserCreatedOn, u.Salt, l.CryptoKeys,
        l.ExpiresOn AS LoginExpiresOn, l.CreatedOn AS LoginCreatedOn
    FROM Users u left join Logins l ON u.Id = l.UserId WHERE Email = @email
GO

DROP PROCEDURE IF EXISTS GetLoginInfoByUserId;
GO

CREATE PROC GetLoginInfoByUserId
(
    @userId UNIQUEIDENTIFIER
)
AS
    SELECT
        l.Id, u.Id AS UserId, u.Email AS UserEmail, u.[Password] AS UserPassword, u.Company AS UserCompany, u.FirstName AS UserFirstName, u.LastName AS UserLastName,
        u.Phone AS UserPhone, u.[Role] AS UserRole, u.IsEmailVerified AS IsUserEmailVerified, u.Picture AS UserPicture, u.CreatedOn AS UserCreatedOn, u.Salt, l.CryptoKeys,
        l.ExpiresOn AS LoginExpiresOn, l.CreatedOn AS LoginCreatedOn
    FROM Users u left join Logins l ON u.Id = l.UserId WHERE u.Id = @userId
GO

DROP PROCEDURE IF EXISTS DeleteLoginByUserId;
GO

CREATE PROC DeleteLoginByUserId
(
    @userId UNIQUEIDENTIFIER
)
AS
    DELETE FROM Logins WHERE UserId = @userId
GO