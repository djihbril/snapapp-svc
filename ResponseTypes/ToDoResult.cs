using Microsoft.Azure.Functions.Worker.Extensions.Sql;
using Microsoft.Azure.Functions.Worker.Http;
using SnapApp.Svc.DbModels;

namespace SnapApp.Svc.ResponseTypes
{
    public class ToDoResult
    {
        [SqlOutput("dbo.ToDo", connectionStringSetting: "SqlConnectionString")]
        public required ToDoItem ToDoItem { get; set; }
        public required HttpResponseData HttpResponse { get; set; }
    }
}