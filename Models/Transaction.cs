namespace SnapApp.Svc.Models;

public struct Transaction
{
    public UserInfo Client { get; set; }
    public PropertyInfo Property { get; set; }

    public readonly void Deconstruct(out UserInfo client, out PropertyInfo property)
    {
        client = Client;
        property = Property;
    }
}