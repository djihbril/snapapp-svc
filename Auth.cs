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

namespace SnapApp.Svc;

public class Auth(ILogger<Auth> logger, IDatabaseService dbContext)
{
    [Function("Login")]
    public async Task<LoginResult> RunLogin([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req, [FromBody] Credentials creds,
        [SqlInput(commandText: "GetLoginInfo", commandType: System.Data.CommandType.StoredProcedure, parameters: "@email={Username}",
        connectionStringSetting: "SqlConnectionString")] IEnumerable<LoginInfo> loginInfos)
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

        if (loginInfos == null || !loginInfos.Any())
        {
            return await DenyAuth();
        }

        LoginInfo loginInfo = loginInfos.First();

        if (creds.Password.HashPassword(loginInfo.Salt) != loginInfo.UserPassword)
        {
            return await DenyAuth();
        }

        DateTime now = DateTime.UtcNow;

        Login login = new()
        {
            UserId = loginInfo.UserId,
            CryptoKeys = loginInfo.CryptoKeys,
            ExpiresOn = now.AddSeconds(Settings.AccessTokenExpirationSpanInSecs),
            CreatedOn = now
        };

        if (loginInfo.LoginCreatedOn != null)
        {
            // Set the new login Id with the existing login id so that the login recorded is updated.
            login.Id = loginInfo.Id;
        }
        else
        {
            login.CryptoKeys = new RSACryptoServiceProvider(2048).ExportCspBlob(true);
        }

        using RSACryptoServiceProvider rsa = new();

        rsa.ImportCspBlob(login.CryptoKeys);
        string accessTokenJson = JsonSerializer.Serialize(new AccessToken
        {
             UserId = loginInfo.UserId,
            Role = loginInfo.UserRole,
            IssuedOn = now
        }, JsonSerializationOptions.CamelCaseNamingOptions);
        string accessToken = Convert.ToBase64String(rsa.Encrypt(Encoding.UTF8.GetBytes(accessTokenJson), false));
        string refreshTokenJson = JsonSerializer.Serialize(new RefreshToken
        {
            UserId = loginInfo.UserId,
            ExpiresOn = now.AddSeconds(Settings.RefreshTokenExpirationSpanInSecs)
        }, JsonSerializationOptions.CamelCaseNamingOptions);
        string refreshToken = Convert.ToBase64String(rsa.Encrypt(Encoding.UTF8.GetBytes(refreshTokenJson), false));

        resp.Headers.Add("Content-Type", "application/json; charset=utf-8");
        resp.StatusCode = HttpStatusCode.OK;
        await resp.WriteStringAsync(JsonSerializer.Serialize(new
        {
            UserInfo = new
            {
                Id = loginInfo.UserId,
                Email = loginInfo.UserEmail,
                Company = loginInfo.UserCompany,
                FirstName = loginInfo.UserFirstName,
                LastName = loginInfo.UserLastName,
                Phone = loginInfo.UserPhone,
                Role = loginInfo.UserRole,
                Picture = loginInfo.UserPicture,
                IsEmailVerified = loginInfo.IsUserEmailVerified,
                CreatedOn = loginInfo.UserCreatedOn
            },
            AccessToken = accessToken,
            RefreshToken = refreshToken
        }, JsonSerializationOptions.CamelCaseNamingOptions));

        return new LoginResult
        {
            Login = login,
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
                Password = string.Empty,
                Company = signUp.Company,
                FirstName = signUp.FirstName,
                LastName = signUp.LastName,
                Phone = signUp.Phone,
                Role = signUp.Role,
                Salt = [],
                CreatedOn = now
            };

            string accessTokenJson = JsonSerializer.Serialize(new AccessToken
            {
                UserId = userId,
                Role = signUp.Role,
                IssuedOn = now
            }, JsonSerializationOptions.CamelCaseNamingOptions);

            string refreshTokenJson = JsonSerializer.Serialize(new RefreshToken
            {
                UserId = userId,
                ExpiresOn = now.AddSeconds(Settings.RefreshTokenExpirationSpanInSecs)
            }, JsonSerializationOptions.CamelCaseNamingOptions);

            using RSACryptoServiceProvider rsa = new(2048);

            user.Salt = CryptoHelpers.GenerateSalt();

            user.Password = signUp.Password.HashPassword(user.Salt);
            string accessToken = Convert.ToBase64String(rsa.Encrypt(Encoding.UTF8.GetBytes(accessTokenJson), false));
            string refreshToken = Convert.ToBase64String(rsa.Encrypt(Encoding.UTF8.GetBytes(refreshTokenJson), false));

            resp.Headers.Add("Content-Type", "application/json; charset=utf-8");
            resp.StatusCode = HttpStatusCode.OK;
            await resp.WriteStringAsync(JsonSerializer.Serialize(new
            {
                UserId = userId,
                UserCreatedOn = now,
                AccessToken = accessToken,
                RefreshToken = refreshTokenJson
            }, JsonSerializationOptions.CamelCaseNamingOptions));

            return new SignUpResult()
            {
                User = user,
                Login = new()
                {
                    UserId = userId,
                    CryptoKeys = rsa.ExportCspBlob(true),
                    ExpiresOn = now.AddSeconds(Settings.AccessTokenExpirationSpanInSecs),
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