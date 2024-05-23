using System.Text.Json.Serialization;

namespace SnapApp.Svc.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserRoles
{
    Client, Realtor
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ClientTypes
{
    Buyer, Seller
}