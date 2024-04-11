namespace SnapApp.Svc.Models;

public struct LoginInfo
{
    public int? Id { get; set; }
    public Guid UserId { get; set; }
    public string UserEmail { get; set; }
    public string UserPassword { get; set; }
    public string UserCompany {get; set; }
    public string UserFirstName { get; set; }
    public string UserLastName { get; set; }
    public string UserPhone { get; set; }
    public UserRoles UserRole { get; set; }
    public bool IsUserEmailVerified { get; set; }
    public string UserPicture { get; set; }
    public DateTime UserCreatedOn { get; set; }
    public byte[] Salt { get; set; }
    public byte[] CryptoKeys { get; set; }
    public DateTime? LoginExpiresOn { get; set; }
    public DateTime? LoginCreatedOn { get; set; }
}