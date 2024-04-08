namespace SnapApp.Svc.DbModels;

public class Login
{
    public int? Id { get; set; }
    public required Guid UserId { get; set; }
    public required byte[] CryptoKeys { get; set; }
    public DateTime ExpiresOn { get; set; }
    public DateTime CreatedOn { get; set; }
}
