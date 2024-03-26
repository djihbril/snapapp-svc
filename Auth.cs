using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
        public async Task<LoginResult> RunLogin([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req, [FromBody] Credentials creds,
            [SqlInput(commandText: "GetLoginInfo", commandType: System.Data.CommandType.StoredProcedure, parameters: "@email={Username}",
            connectionStringSetting: "SqlConnectionString")] IEnumerable<LoginInfo> logins)
        {
            logger.LogInformation("[Login] function processed a request.");

            HttpResponseData resp = req.CreateResponse();

            async Task<LoginResult> DenyAuth()
            {
                resp.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                resp.StatusCode = HttpStatusCode.Unauthorized;
                await resp.WriteStringAsync("Invalid username or password.");

                return new LoginResult
                {
                    HttpResponse = resp
                };
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

            DateTime now = DateTime.UtcNow;

            Login newLogin = new()
            {
                UserId = login.UserId,
                CryptoKeys = login.CryptoKeys,
                ExpiresOn = now.AddSeconds(Settings.TokenExpirationSpanInSecs),
                CreatedOn = now
            };

            AccessToken accessToken = new()
            {
                UserId = login.UserId,
                Role = login.UserRole,
                IssuedOn = now
            };

            if (login.LoginCreatedOn > DateTime.MinValue)
            {
                newLogin.Id = login.Id;
                newLogin.CreatedOn = login.LoginCreatedOn;
                accessToken.IssuedOn = login.LoginCreatedOn;
            }
            else
            {
                newLogin.CryptoKeys = new RSACryptoServiceProvider(2048).ExportCspBlob(true);
            }

            using RSACryptoServiceProvider rsa = new();
            rsa.ImportCspBlob(newLogin.CryptoKeys);

            string tokenJson = JsonSerializer.Serialize(accessToken, JsonSerializationOptions.CamelCaseNamingOptions);
            string token = Convert.ToBase64String(rsa.Encrypt(Encoding.UTF8.GetBytes(tokenJson), false));

            resp.Headers.Add("Content-Type", "application/json; charset=utf-8");
            resp.StatusCode = HttpStatusCode.OK;
            await resp.WriteStringAsync(JsonSerializer.Serialize(new
            {
                UserInfo = new
                {
                    Email = login.UserEmail,
                    Company = login.UserCompany,
                    FirstName = login.UserFirstName,
                    LastName = login.UserLastName,
                    Phone = login.UserPhone,
                    Role = login.UserRole,
                    Picture = login.UserPicture,
                    IsEmailVerified = login.IsUserEmailVerified,
                    CreatedOn = login.UserCreatedOn
                },
                AccessToken = token
            }, JsonSerializationOptions.CamelCaseNamingOptions));

            return new LoginResult
            {
                Login = newLogin,
                HttpResponse = resp
            };
        }

        [Function("SignUp")]
        public async Task<SignUpResult> RunSignUp([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req, [FromBody] SignUp signUp,
            [SqlInput(commandText: "CheckExistingUser", commandType: System.Data.CommandType.StoredProcedure, parameters: "@email={Email}",
            connectionStringSetting: "SqlConnectionString")] string scalarJson)
        {
            logger.LogInformation("[SignUp] function processed a request.");

            var scalarObj = JsonSerializer.Deserialize<object>(scalarJson);
            bool userExists = scalarObj is JsonElement jEle && jEle[0].GetProperty("UserExists").GetInt32() != 0;
            HttpResponseData resp = req.CreateResponse();

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
                    CreatedOn = now
                };

                string tokenJson = JsonSerializer.Serialize(new AccessToken
                {
                    UserId = userId,
                    Role = signUp.Role,
                    IssuedOn = now
                }, JsonSerializationOptions.CamelCaseNamingOptions);

                using RSACryptoServiceProvider rsa = new(2048);

                user.Salt = CryptoHelpers.GenerateSalt();

                user.Password = signUp.Password.HashPassword(user.Salt);
                string token = Convert.ToBase64String(rsa.Encrypt(Encoding.UTF8.GetBytes(tokenJson), false));

                resp.Headers.Add("Content-Type", "application/json; charset=utf-8");
                resp.StatusCode = HttpStatusCode.OK;
                await resp.WriteStringAsync(JsonSerializer.Serialize(new
                {
                    UserCreatedOn = now,
                    AccessToken = token
                }, JsonSerializationOptions.CamelCaseNamingOptions));

                return new SignUpResult()
                {
                    User = user,
                    Login = new()
                    {
                        UserId = userId,
                        CryptoKeys = rsa.ExportCspBlob(true),
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