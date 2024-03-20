namespace SnapApp.Svc.Models
{
    public class AccessToken
    {
        public required Guid UserId { get; set; }
        public required UserRoles Role { get; set; }
        public required DateTime IssuedOn { get; set; }
    }
}