using SnapApp.Svc.Models;

namespace SnapApp.Svc.Middleware;

/// <summary>
/// Function Authorize attribute.
/// Contains zero or more roles.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class FunctionAuthorizeAttribute(params UserRoles[] roles) : Attribute
{
    public IEnumerable<UserRoles> Roles { get; } = roles;
}