namespace SnapApp.Svc.DbModels;

public class Invitation
{
    public int? Id { get; set; }
    public required Guid ClientId { get; set; }
    public required string Code { get; set; }
    public DateTime CreatedOn { get; set; }
}