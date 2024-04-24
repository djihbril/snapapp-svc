namespace SnapApp.Svc.Models;

public class SignUp
{
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string Company {get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Phone { get; set; }
    public UserRoles Role { get; set; }
    public string? Picture { get; set; }
}