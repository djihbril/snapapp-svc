using System.Text.Json.Serialization;

namespace SnapApp.Svc.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserRoles
{
    Client, Realtor
}

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