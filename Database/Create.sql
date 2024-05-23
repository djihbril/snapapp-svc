IF OBJECT_ID('dbo.Logins', 'U') IS NOT NULL
BEGIN
    DELETE FROM Logins;
END
GO

IF OBJECT_ID('dbo.Properties', 'U') IS NOT NULL
BEGIN
    DELETE FROM Properties;
END
GO

IF OBJECT_ID('dbo.Invitations', 'U') IS NOT NULL
BEGIN
    DELETE FROM Invitations;
END
GO

IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL
BEGIN
    DELETE FROM Users;
END
GO

DROP TABLE IF EXISTS Logins;
GO

DROP TABLE IF EXISTS Properties;
GO

DROP TABLE IF EXISTS Invitations;
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
    [RefreshTokenId] UNIQUEIDENTIFIER NOT NULL,
    [ExpiresOn] DATETIME2 DEFAULT GETUTCDATE(),
    [CreatedOn] DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY(UserId) REFERENCES Users(Id)
);
GO

CREATE TABLE Properties
(
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [ClientId] UNIQUEIDENTIFIER NOT NULL,
    [RealtorId] UNIQUEIDENTIFIER NOT NULL,
    [ClientType] VARCHAR(10) NOT NULL,
    [Address1] VARCHAR(150) NOT NULL,
    [Address2] VARCHAR(150),
    [City] VARCHAR(100) NOT NULL,
    [State] VARCHAR(50) NOT NULL,
    [ZipCode] VARCHAR(10) NOT NULL,
    [ListedOn] DATETIME2,
    [ListingExpiresOn] DATETIME2,
    [ContractAcceptedOn] DATETIME2,
    [DueDiligenceExpiresOn] DATETIME2,
    [ClosesOn] DATETIME2,
    [CreatedOn] DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY(ClientId) REFERENCES Users(Id)
);
GO

CREATE TABLE Invitations
(
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [ClientId] UNIQUEIDENTIFIER NOT NULL,
    [Code] VARCHAR(10) NOT NULL,
    [CreatedOn] DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY(ClientId) REFERENCES Users(Id)
);
GO

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

DROP PROCEDURE IF EXISTS CheckExistingUserById;
GO

CREATE PROC CheckExistingUserById
(
    @id UNIQUEIDENTIFIER
)
AS
    SELECT
        CASE
            WHEN EXISTS (SELECT 1 FROM Users WHERE Id = @id)
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
        u.Phone AS UserPhone, u.[Role] AS UserRole, u.IsEmailVerified AS IsUserEmailVerified, u.Picture AS UserPicture, u.CreatedOn AS UserCreatedOn, l.RefreshTokenId,
        u.Salt, l.CryptoKeys, l.ExpiresOn AS LoginExpiresOn, l.CreatedOn AS LoginCreatedOn
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
        u.Phone AS UserPhone, u.[Role] AS UserRole, u.IsEmailVerified AS IsUserEmailVerified, u.Picture AS UserPicture, u.CreatedOn AS UserCreatedOn, l.RefreshTokenId,
        u.Salt, l.CryptoKeys, l.ExpiresOn AS LoginExpiresOn, l.CreatedOn AS LoginCreatedOn
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

DROP PROCEDURE IF EXISTS InsertLogin;
GO

CREATE PROC InsertLogin
(
    @id INT OUTPUT,
    @userId UNIQUEIDENTIFIER,
    @cryptoKeys BINARY(1172),
    @refreshTokenId UNIQUEIDENTIFIER,
    @expiresOn DATETIME2,
    @createdOn DATETIME2
)
AS
    INSERT INTO Logins (UserId, CryptoKeys, RefreshTokenId, ExpiresOn, CreatedOn) VALUES (@userId, @cryptoKeys, @refreshTokenId, @expiresOn, @CreatedOn);
    SET @id = SCOPE_IDENTITY();
GO

DROP PROCEDURE IF EXISTS UpdateLogin;
GO

CREATE PROC UpdateLogin
(
    @id INT,
    @userId UNIQUEIDENTIFIER,
    @cryptoKeys BINARY(1172),
    @refreshTokenId UNIQUEIDENTIFIER,
    @expiresOn DATETIME2,
    @createdOn DATETIME2
)
AS
    UPDATE Logins SET
        UserId = @userId,
        CryptoKeys = @cryptoKeys,
        RefreshTokenId = @refreshTokenId,
        ExpiresOn = @expiresOn,
        CreatedOn = @createdOn
    WHERE Id = @id;
GO

DROP PROCEDURE IF EXISTS GetUserInfoById;
GO

CREATE PROC GetUserInfoById
(
    @id UNIQUEIDENTIFIER
)
AS
    SELECT
        Id, Email, [Password], Company, FirstName, LastName, Phone, [Role], IsEmailVerified, Picture, Salt, CreatedOn
    FROM Users WHERE Id = @id
GO

DROP PROCEDURE IF EXISTS AcceptInvitation;
GO

CREATE PROC AcceptInvitation
(
    @code VARCHAR(10)
)
AS
    DECLARE @codeExists BIT, @clientId UNIQUEIDENTIFIER;

    SELECT @codeExists =
        CASE
            WHEN EXISTS (SELECT 1 FROM Invitations WHERE Code = @code COLLATE SQL_Latin1_General_CP1_CS_AS)
            THEN 1 ELSE 0
        END;

    IF @codeExists = 1
    BEGIN
        SELECT @clientId = ClientId FROM Invitations WHERE Code = @code COLLATE SQL_Latin1_General_CP1_CS_AS;
        Update Users SET IsEmailVerified = 1 WHERE Id = @clientId;
        DELETE FROM Invitations WHERE Code = @code;
    END

    SELECT @codeExists AS CodeExists;
GO

DROP PROCEDURE IF EXISTS CheckExistingProperty;
GO

SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROC CheckExistingProperty
(
    @address1 NVARCHAR(150),
    @address2 NVARCHAR(150),
    @city VARCHAR(100),
    @state VARCHAR(50),
    @zipCode VARCHAR(10)
)
AS
    SELECT
        CASE
            WHEN EXISTS (SELECT 1 FROM Properties WHERE Address1 = @address1 AND Address2 = @address2 AND City = @city AND ZipCode = @zipCode)
            THEN 1 ELSE 0
        END AS PropertyExists;
GO