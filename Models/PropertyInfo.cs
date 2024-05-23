namespace SnapApp.Svc.Models;

public struct PropertyInfo
{
    public int? Id { get; set; }
    public Guid ClientId { get; set; }
    public Guid RealtorId { get; set; }
    public ClientTypes ClientType { get; set; }
    public string Address1 { get; set; }
    public string Address2 { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string ZipCode { get; set; }
    public DateTime? ListedOn { get; set; }
    public DateTime? ListingExpiresOn { get; set; }
    public DateTime? ContractAcceptedOn { get; set; }
    public DateTime? DueDiligenceExpiresOn { get; set; }
    public DateTime? ClosesOn { get; set; }
    public DateTime CreatedOn { get; set; }

    public readonly bool HasRequiredData
    {
        get => !(string.IsNullOrWhiteSpace(Address1) || string.IsNullOrWhiteSpace(City) || string.IsNullOrWhiteSpace(State) || string.IsNullOrWhiteSpace(ZipCode) ||
            RealtorId == Guid.Empty);

    }
}