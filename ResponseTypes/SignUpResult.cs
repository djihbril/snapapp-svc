using Microsoft.Azure.Functions.Worker.Extensions.Sql;
using Microsoft.Azure.Functions.Worker.Http;
using SnapApp.Svc.DbModels;

namespace SnapApp.Svc.ResponseTypes;

public class SignUpResult
{
    [SqlOutput("dbo.Users", connectionStringSetting: "SqlConnectionString")]
    public User? User { get; set; }
    [SqlOutput("dbo.Logins", connectionStringSetting: "SqlConnectionString")]
    public Login? Login { get; set; }
    public required HttpResponseData HttpResponse { get; set; }
}
