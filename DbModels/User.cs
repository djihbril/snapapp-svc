using SnapApp.Svc.Models;

namespace SnapApp.Svc.DbModels;

public class User
{
    public required Guid Id { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string Company {get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Phone { get; set; }
    public required UserRoles Role { get; set; }
    public bool IsEmailVerified { get; set; }
    public string? Picture { get; set; }
    public required byte[] Salt { get; set; }
    public DateTime CreatedOn { get; set; }
}
