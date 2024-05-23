using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Sql;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SnapApp.Svc.Extensions;
using SnapApp.Svc.Middleware;
using SnapApp.Svc.Models;
using SnapApp.Svc.ResponseTypes;
using SnapApp.Svc.Services;

namespace SnapApp.Svc;

public class Api(ILogger<Api> logger/*, IDatabaseService dbContext*/, IEmailCommunicationService emailService)
{
    [FunctionAuthorize(UserRoles.Realtor)]
    [Function("AddClient")]
    public async Task<UserResult> RunAddClient([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req, [FromBody] UserInfo userInfo,
        [SqlInput(commandText: "CheckExistingUser", commandType: System.Data.CommandType.StoredProcedure, parameters: "@email={Email}",
        connectionStringSetting: "SqlConnectionString")] string scalarJson)
    {
        logger.LogInformation("[AddClient] function processed a request.");

        var scalarObj = JsonSerializer.Deserialize<object>(scalarJson);
        bool userExists = scalarObj is JsonElement jEle && jEle[0].GetProperty("UserExists").GetInt32() != 0;
        HttpResponseData resp = req.CreateResponse();
        ClaimsPrincipal user = req.FunctionContext.GetHttpContext()!.User;

        if (!user.Identity!.IsAuthenticated)
        {
            resp.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            resp.StatusCode = HttpStatusCode.Unauthorized;
            await resp.WriteStringAsync("Access Unauthorized.");

            return new UserResult()
            {
                HttpResponse = resp
            };
        }

        if (userExists)
        {
            resp.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            resp.StatusCode = HttpStatusCode.Conflict;
            await resp.WriteStringAsync($"User {userInfo.Email} already exists.");

            return new UserResult()
            {
                HttpResponse = resp
            };
        }

        if (userInfo.HasRequiredData)
        {
            string fName = user.Claims.First(c => c.Type == "firstName").Value;
            string lName = user.Claims.First(c => c.Type == "lastName").Value;
            string companyName = user.Claims.First(c => c.Type == "company").Value;
            string inviteCode = InvitationCodeGenerator.Generate();
            var templateData = new
            {
                logoLink = Settings.LogoLink,
                recipientName = $"{userInfo.FirstName}",
                realtorName = $"{fName} {lName}",
                appStoreLink = Settings.AppStoreLink,
                senderAddress = Settings.EmailSenderAddress,
                companyName,
                inviteCode
            };

            string emailHtmlContent = EmailTemplates.inviteEmail.Format(templateData);

            if (Settings.EmailSenderAddress != null &&
                emailService.SendAsync(Settings.EmailSenderAddress, userInfo.Email, "You've be invited to SnapApp", emailHtmlContent).Result)
            {
                DateTime now = DateTime.UtcNow;
                Guid userId = Guid.NewGuid();
                byte[] salt = CryptoHelpers.GenerateSalt();

                resp.Headers.Add("Content-Type", "application/json; charset=utf-8");
                resp.StatusCode = HttpStatusCode.Created;
                await resp.WriteStringAsync("Client added.");

                return new UserResult()
                {
                    User = new()
                    {
                        Id = userId,
                        Email = userInfo.Email,
                        Password = userInfo.Password.HashPassword(salt),
                        Company = userInfo.Company,
                        FirstName = userInfo.FirstName,
                        LastName = userInfo.LastName,
                        Phone = userInfo.Phone,
                        Role = userInfo.Role,   // Always Client.
                        Salt = salt,
                        CreatedOn = now
                    },
                    Invitation = new()
                    {
                        ClientId = userId,
                        Code = inviteCode
                    },
                    HttpResponse = resp
                };
            }

            resp.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            resp.StatusCode = HttpStatusCode.InternalServerError;
            await resp.WriteStringAsync("Unable to send invitation email.");

            return new UserResult()
            {
                HttpResponse = resp
            };
        }
        else
        {
            resp.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            resp.StatusCode = HttpStatusCode.NotFound;
            await resp.WriteStringAsync("Missing client request content.");

            return new UserResult()
            {
                HttpResponse = resp
            };
        }
    }

    [FunctionAuthorize(UserRoles.Realtor)]
    [Function("AddProperty")]
    public async Task<PropertyResult> RunAddProperty([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req, [FromBody] PropertyInfo propInfo,
        [SqlInput(commandText: "CheckExistingUserById", commandType: System.Data.CommandType.StoredProcedure, parameters: "@id={ClientId}",
        connectionStringSetting: "SqlConnectionString")] string clientScalarJson,
        [SqlInput(commandText: "CheckExistingProperty", commandType: System.Data.CommandType.StoredProcedure, connectionStringSetting: "SqlConnectionString",
        parameters: "@address1={Property.Address1}, @address2={Address2}, @city={City}, @state={State}, @zipCode={ZipCode}")]
        string propertyScalarJson)
    {
        logger.LogInformation("[AddProperty] function processed a request.");

        var clientScalarObj = JsonSerializer.Deserialize<object>(clientScalarJson);
        bool clientExists = clientScalarObj is JsonElement uJEle && uJEle[0].GetProperty("UserExists").GetInt32() != 0;
        var propertyScalarObj = JsonSerializer.Deserialize<object>(propertyScalarJson);
        bool propertyExists = propertyScalarObj is JsonElement pJEle && pJEle[0].GetProperty("PropertyExists").GetInt32() != 0;
        HttpResponseData resp = req.CreateResponse();
        ClaimsPrincipal user = req.FunctionContext.GetHttpContext()!.User;

        if (!user.Identity!.IsAuthenticated)
        {
            resp.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            resp.StatusCode = HttpStatusCode.Unauthorized;
            await resp.WriteStringAsync("Access Unauthorized.");

            return new PropertyResult()
            {
                HttpResponse = resp
            };
        }

        if (!clientExists || propertyExists)
        {
            resp.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            resp.StatusCode = !clientExists ? HttpStatusCode.NotFound : HttpStatusCode.Conflict;
            string propAddress2 = propInfo.Address2 != null && !string.IsNullOrWhiteSpace(propInfo.Address2) ? $", {propInfo.Address2}" : string.Empty;
            string msg = !clientExists ? "Client doesn't exist." :
            $"Property at {propInfo.Address1}{propAddress2}, {propInfo.City}, {propInfo.State} {propInfo.ZipCode} already exists.";
            await resp.WriteStringAsync(msg);

            return new PropertyResult()
            {
                HttpResponse = resp
            };
        }

        Guid claimUserId = Guid.Parse(user.Claims.First(c => c.Type == "userId").Value);

        if (propInfo.HasRequiredData && claimUserId == propInfo.RealtorId)
        {
            DateTime now = DateTime.UtcNow;

            resp.Headers.Add("Content-Type", "application/json; charset=utf-8");
            resp.StatusCode = HttpStatusCode.Created;
            await resp.WriteStringAsync("Transaction added.");

            return new PropertyResult()
            {
                Property = new()
                {
                    ClientId = propInfo.ClientId,
                    RealtorId = propInfo.RealtorId,
                    ClientType = propInfo.ClientType,
                    Address1 = propInfo.Address1,
                    Address2 = propInfo.Address2,
                    City = propInfo.City,
                    State = propInfo.State,
                    ZipCode = propInfo.ZipCode,
                    ListedOn = propInfo.ListedOn,
                    ListingExpiresOn = propInfo.ListingExpiresOn,
                    ContractAcceptedOn = propInfo.ContractAcceptedOn,
                    DueDiligenceExpiresOn = propInfo.DueDiligenceExpiresOn,
                    ClosesOn = propInfo.ClosesOn,
                    CreatedOn = now
                },
                HttpResponse = resp
            };
        }
        else
        {
            resp.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            resp.StatusCode = !propInfo.HasRequiredData ? HttpStatusCode.NotFound : HttpStatusCode.Conflict;
            await resp.WriteStringAsync(!propInfo.HasRequiredData ? "Missing transaction request content." : "Claim user is not the realtor.");

            return new PropertyResult()
            {
                HttpResponse = resp
            };
        }
    }

    [FunctionAuthorize(UserRoles.Realtor)]
    [Function("AddTransaction")]
    public async Task<TransactionResult> RunAddTransaction([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req, [FromBody] Transaction transaction,
        [SqlInput(commandText: "CheckExistingUser", commandType: System.Data.CommandType.StoredProcedure, parameters: "@email={Client.Email}",
        connectionStringSetting: "SqlConnectionString")] string clientScalarJson,
        [SqlInput(commandText: "CheckExistingProperty", commandType: System.Data.CommandType.StoredProcedure, connectionStringSetting: "SqlConnectionString",
        parameters: "@address1={Property.Address1}, @address2={Property.Address2}, @city={Property.City}, @state={Property.State}, @zipCode={Property.ZipCode}")]
        string propertyScalarJson)
    {
        logger.LogInformation("[AddTransaction] function processed a request.");

        var clientScalarObj = JsonSerializer.Deserialize<object>(clientScalarJson);
        bool clientExists = clientScalarObj is JsonElement uJEle && uJEle[0].GetProperty("UserExists").GetInt32() != 0;
        var propertyScalarObj = JsonSerializer.Deserialize<object>(propertyScalarJson);
        bool propertyExists = propertyScalarObj is JsonElement pJEle && pJEle[0].GetProperty("PropertyExists").GetInt32() != 0;
        HttpResponseData resp = req.CreateResponse();
        ClaimsPrincipal user = req.FunctionContext.GetHttpContext()!.User;

        if (!user.Identity!.IsAuthenticated)
        {
            resp.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            resp.StatusCode = HttpStatusCode.Unauthorized;
            await resp.WriteStringAsync("Access Unauthorized.");

            return new TransactionResult()
            {
                HttpResponse = resp
            };
        }

        var (clientInfo, propInfo) = transaction;

        if (clientExists || propertyExists)
        {
            resp.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            resp.StatusCode = HttpStatusCode.Conflict;
            string propAddress2 = propInfo.Address2 != null && !string.IsNullOrWhiteSpace(propInfo.Address2) ? $", {propInfo.Address2}" : string.Empty;
            string msg = clientExists ? $"Client {clientInfo.Email} already exists." :
            $"Property at {propInfo.Address1}{propAddress2}, {propInfo.City}, {propInfo.State} {propInfo.ZipCode} already exists.";
            await resp.WriteStringAsync(msg);

            return new TransactionResult()
            {
                HttpResponse = resp
            };
        }

        Guid claimUserId = Guid.Parse(user.Claims.First(c => c.Type == "userId").Value);

        if (clientInfo.HasRequiredData && propInfo.HasRequiredData && claimUserId == propInfo.RealtorId)
        {
            string fName = user.Claims.First(c => c.Type == "firstName").Value;
            string lName = user.Claims.First(c => c.Type == "lastName").Value;
            string companyName = user.Claims.First(c => c.Type == "company").Value;
            string inviteCode = InvitationCodeGenerator.Generate();
            var templateData = new
            {
                logoLink = Settings.LogoLink,
                recipientName = $"{clientInfo.FirstName}",
                realtorName = $"{fName} {lName}",
                appStoreLink = Settings.AppStoreLink,
                senderAddress = Settings.EmailSenderAddress,
                companyName,
                inviteCode
            };

            string emailHtmlContent = EmailTemplates.inviteEmail.Format(templateData);

            if (Settings.EmailSenderAddress != null &&
                emailService.SendAsync(Settings.EmailSenderAddress, clientInfo.Email, "You've be invited to SnapApp", emailHtmlContent).Result)
            {
                DateTime now = DateTime.UtcNow;
                Guid userId = Guid.NewGuid();
                byte[] salt = CryptoHelpers.GenerateSalt();

                resp.Headers.Add("Content-Type", "application/json; charset=utf-8");
                resp.StatusCode = HttpStatusCode.Created;
                await resp.WriteStringAsync("Transaction added.");

                return new TransactionResult()
                {
                    User = new()
                    {
                        Id = userId,
                        Email = clientInfo.Email,
                        Password = clientInfo.Password.HashPassword(salt),
                        Company = clientInfo.Company,
                        FirstName = clientInfo.FirstName,
                        LastName = clientInfo.LastName,
                        Phone = clientInfo.Phone,
                        Role = clientInfo.Role,   // Always Client.
                        Salt = salt,
                        CreatedOn = now
                    },
                    Property = new()
                    {
                        ClientId = userId,
                        RealtorId = propInfo.RealtorId,
                        ClientType = propInfo.ClientType,
                        Address1 = propInfo.Address1,
                        Address2 = propInfo.Address2,
                        City = propInfo.City,
                        State = propInfo.State,
                        ZipCode = propInfo.ZipCode,
                        ListedOn = propInfo.ListedOn,
                        ListingExpiresOn = propInfo.ListingExpiresOn,
                        ContractAcceptedOn = propInfo.ContractAcceptedOn,
                        DueDiligenceExpiresOn = propInfo.DueDiligenceExpiresOn,
                        ClosesOn = propInfo.ClosesOn,
                        CreatedOn = now
                    },

                    Invitation = new()
                    {
                        ClientId = userId,
                        Code = inviteCode
                    },
                    HttpResponse = resp
                };
            }

            resp.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            resp.StatusCode = HttpStatusCode.InternalServerError;
            await resp.WriteStringAsync("Unable to send invitation email.");

            return new TransactionResult()
            {
                HttpResponse = resp
            };
        }
        else
        {
            resp.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            resp.StatusCode = !clientInfo.HasRequiredData || !propInfo.HasRequiredData ? HttpStatusCode.NotFound : HttpStatusCode.Conflict;
            await resp.WriteStringAsync(!clientInfo.HasRequiredData || !propInfo.HasRequiredData ?
                "Missing transaction request content." : "Claim user is not the realtor.");

            return new TransactionResult()
            {
                HttpResponse = resp
            };
        }
    }

    [FunctionAuthorize]
    [Function("Test")]
    public async Task<HttpResponseData> RunTest([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        logger.LogInformation("[Test] function processed a request.");

        HttpResponseData resp = req.CreateResponse();
        ClaimsPrincipal user = req.FunctionContext.GetHttpContext()!.User;
        resp.Headers.Add("Content-Type", "text/plain; charset=utf-8");

        if (user.Identity!.IsAuthenticated)
        {
            resp.StatusCode = HttpStatusCode.OK;
            await resp.WriteStringAsync("Authenticated request from test.");
        }
        else
        {
            resp.StatusCode = HttpStatusCode.Unauthorized;
            await resp.WriteStringAsync("Test request unauthorized.");
        }

        return resp;
    }
}