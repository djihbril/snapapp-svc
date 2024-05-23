using Microsoft.Azure.Functions.Worker.Extensions.Sql;
using Microsoft.Azure.Functions.Worker.Http;
using SnapApp.Svc.DbModels;

namespace SnapApp.Svc.ResponseTypes;

public class PropertyResult
{
    [SqlOutput("dbo.Properties", connectionStringSetting: "SqlConnectionString")]
    public Property? Property { get; set; }
    public required HttpResponseData HttpResponse { get; set; }
}