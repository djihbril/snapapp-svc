namespace SnapApp.Svc.Models;

public struct UserInfo
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string Company {get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Phone { get; set; }
    public UserRoles Role { get; set; }
    public bool IsEmailVerified { get; set; }
    public string Picture { get; set; }
    public DateTime CreatedOn { get; set; }
}