using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SnapApp.Svc.Middleware;
using SnapApp.Svc.Extensions;
using SnapApp.Svc.Services;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using SnapApp.Svc;
using Azure.Communication.Email;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication(configure =>
    {
        configure.UseWhen<BearerAuthenticationMiddleware>(context =>
        {
            FunctionDefinition fnDef = context.FunctionDefinition;
            FunctionAuthorizeAttribute? fnAttr = fnDef.GetFunctionAuthorizeAttribute();

            return fnAttr != null && fnDef.InputBindings.Values.First(a => a.Type.EndsWith("Trigger")).Type == "httpTrigger";
        });
    })
    .ConfigureServices(services => {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        services.AddScoped<DbConnection>(provider => new SqlConnection(Settings.SqlConnectionString));
        services.AddScoped<IDatabaseService, DatabaseContext>();

        services.AddSingleton(provider => new EmailClient(Settings.ComServicesConnectionString));
        services.AddSingleton<IEmailCommunicationService, EmailService>();
    })
    .Build();

host.Run();
