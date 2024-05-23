using Microsoft.Azure.Functions.Worker.Extensions.Sql;
using Microsoft.Azure.Functions.Worker.Http;
using SnapApp.Svc.DbModels;

namespace SnapApp.Svc.ResponseTypes;

public class TransactionResult
{
    [SqlOutput("dbo.Users", connectionStringSetting: "SqlConnectionString")]
    public User? User { get; set; }
    [SqlOutput("dbo.Properties", connectionStringSetting: "SqlConnectionString")]
    public Property? Property { get; set; }
    [SqlOutput("dbo.Invitations", connectionStringSetting: "SqlConnectionString")]
    public Invitation? Invitation { get; set; }
    public required HttpResponseData HttpResponse { get; set; }
}