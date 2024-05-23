using SnapApp.Svc.Models;

namespace SnapApp.Svc.DbModels;

public class Property
{
    public int? Id { get; set; }
    public required Guid ClientId { get; set; }
    public required Guid RealtorId { get; set; }
    public required ClientTypes ClientType { get; set; }
    public required string Address1 { get; set; }
    public string? Address2 { get; set; }
    public required string City { get; set; }
    public required string State { get; set; }
    public required string ZipCode { get; set; }
    public DateTime? ListedOn { get; set; }
    public DateTime? ListingExpiresOn { get; set; }
    public DateTime? ContractAcceptedOn { get; set; }
    public DateTime? DueDiligenceExpiresOn { get; set; }
    public DateTime? ClosesOn { get; set; }
    public DateTime CreatedOn { get; set; }
}