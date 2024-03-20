using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Sql;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SnapApp.Svc.DbModels;
using SnapApp.Svc.Extensions;
using SnapApp.Svc.Models;
using SnapApp.Svc.ResponseTypes;
using FromBodyAttribute = Microsoft.Azure.Functions.Worker.Http.FromBodyAttribute;

namespace SnapApp.Svc
{
    public class Auth(ILogger<Auth> logger)
    {
        [Function("Login")]
        public async Task<HttpResponseData> RunLogin([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req, [FromBody] Credentials creds,
            [SqlInput(commandText: "GetLoginInfo", commandType: System.Data.CommandType.StoredProcedure, parameters: "@email={Username}",
            connectionStringSetting: "SqlConnectionString")] IEnumerable<LoginInfo> logins)
        {
            logger.LogInformation("[Login] function processed a request.");

            Guid deviceId;
            HttpResponseData resp = req.CreateResponse();

            async Task<HttpResponseData> DenyAuth()
            {
                resp.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                resp.StatusCode = HttpStatusCode.Unauthorized;
                await resp.WriteStringAsync("Invalid username or password.");

                return resp;
            }

            if (logins == null || !logins.Any())
            {
                return await DenyAuth();
            }

            LoginInfo login = logins.First();

            if (creds.Password.HashPassword(login.Salt) != login.UserPassword)
            {
                return await DenyAuth();
            }

            if (req.Headers.TryGetValues("X-Device-Id", out IEnumerable<string>? values) && values != null)
            {
                string deviceIdStr = Encoding.UTF8.GetString(Convert.FromBase64String(values.First()));
                deviceId = Guid.Parse(deviceIdStr.DeObfuscate());
            }
            else
            {
                resp.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                resp.StatusCode = HttpStatusCode.NotFound;
                await resp.WriteStringAsync("Missing device id.");

                return resp;
            }

            if (login.LoginCreatedOn > DateTime.MinValue)
            {
                // TODO: Update login.
            }
            else
            {
                // TODO: Add login.
            }
        }

        [Function("SignUp")]
        public async Task<SignUpResult> RunSignUp([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req, [FromBody] SignUp signUp,
            [SqlInput(commandText: "CheckExistingUser", commandType: System.Data.CommandType.StoredProcedure, parameters: "@email={Email}",
            connectionStringSetting: "SqlConnectionString")] string scalarJson)
        {
            logger.LogInformation("[SignUp] function processed a request.");

            Guid deviceId;
            var scalarObj = JsonSerializer.Deserialize<object>(scalarJson);
            bool userExists = scalarObj is JsonElement jEle && jEle[0].GetProperty("UserExists").GetInt32() != 0;
            HttpResponseData resp = req.CreateResponse();

            if (req.Headers.TryGetValues("X-Device-Id", out IEnumerable<string>? values) && values != null)
            {
                string deviceIdStr = Encoding.UTF8.GetString(Convert.FromBase64String(values.First()));
                deviceId = Guid.Parse(deviceIdStr.DeObfuscate());
            }
            else
            {
                resp.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                resp.StatusCode = HttpStatusCode.NotFound;
                await resp.WriteStringAsync("Missing device id.");

                return new SignUpResult()
                {
                    HttpResponse = resp
                };
            }

            if (userExists)
            {
                resp.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                resp.StatusCode = HttpStatusCode.Conflict;
                await resp.WriteStringAsync($"User {signUp.Email} already exists.");

                return new SignUpResult()
                {
                    HttpResponse = resp
                };
            }

            if (signUp != null)
            {
                DateTime now = DateTime.UtcNow;
                Guid userId = Guid.NewGuid();

                User user = new()
                {
                    Id = userId,
                    Email = signUp.Email,
                    Password = "",
                    Company = signUp.Company,
                    FirstName = signUp.FirstName,
                    LastName = signUp.LastName,
                    Phone = signUp.Phone,
                    Role = signUp.Role,
                    Salt = [],
                    CryptoKeys = [],
                    CreatedOn = now
                };

                string tokenJson = JsonSerializer.Serialize(new AccessToken
                {
                    UserId = userId,
                    IssuedOn = now,
                    // ExpiresIn = Settings.TokenExpirationSpanInSecs,
                    DeviceId = deviceId
                }, JsonSerializationOptions.CamelCaseNamingOptions);

                using RSACryptoServiceProvider rsa = new(2048);

                user.Salt = CryptoHelpers.GenerateSalt();
                user.CryptoKeys = rsa.ExportCspBlob(true);
                
                user.Password = signUp.Password.HashPassword(user.Salt);
                string token = Convert.ToBase64String(rsa.Encrypt(Encoding.UTF8.GetBytes(tokenJson), false));

                resp.Headers.Add("Content-Type", "application/json; charset=utf-8");
                resp.StatusCode = HttpStatusCode.OK;
                await resp.WriteStringAsync(JsonSerializer.Serialize(new
                {
                    UserId = userId,
                    UserCreatedOn = now,
                    AccessToken = token,
                    EncryptionKey = Convert.ToBase64String(rsa.ExportCspBlob(false)),
                }, JsonSerializationOptions.CamelCaseNamingOptions));

                return new SignUpResult()
                {
                    User = user,
                    Login = new()
                    {
                        UserId = userId,
                        DeviceId = deviceId,
                        ExpiresOn = now.AddSeconds(Settings.TokenExpirationSpanInSecs),
                        CreatedOn = now
                    },
                    HttpResponse = resp
                };
            }
            else
            {
                resp.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                resp.StatusCode = HttpStatusCode.NotFound;
                await resp.WriteStringAsync("Missing signup request content.");

                return new SignUpResult()
                {
                    HttpResponse = resp
                };
            }
        }
    }
}