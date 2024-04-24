using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Sql;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SnapApp.Svc.DbModels;
using SnapApp.Svc.Extensions;
using SnapApp.Svc.Middleware;
using SnapApp.Svc.Models;
using SnapApp.Svc.ResponseTypes;
using SnapApp.Svc.Services;
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
        Guid refreshTokenId = Guid.NewGuid();

        Login login = new()
        {
            UserId = loginInfo.UserId,
            CryptoKeys = loginInfo.CryptoKeys,
            RefreshTokenId = refreshTokenId,
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
            Id = refreshTokenId,
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
            Guid refreshTokenId = Guid.NewGuid();

            User user = new()
            {
                Id = userId,
                Email = signUp.Email,
                Password = string.Empty,
                Company = signUp.Company,
                FirstName = signUp.FirstName,
                LastName = signUp.LastName,
                Phone = signUp.Phone,
                Role = signUp.Role,     // Always Realtor.
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
                Id = refreshTokenId,
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
                RefreshToken = refreshToken
            }, JsonSerializationOptions.CamelCaseNamingOptions));

            return new SignUpResult()
            {
                User = user,
                Login = new()
                {
                    UserId = userId,
                    CryptoKeys = rsa.ExportCspBlob(true),
                    RefreshTokenId = refreshTokenId,
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

    [Function("RenewToken")]
    public async Task<HttpResponseData> RunRenewToken([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        logger.LogInformation("[RenewToken] function processed a request.");

        HttpResponseData resp = req.CreateResponse();

        if (req.Headers.TryGetValues("Authorization", out IEnumerable<string>? values))
        {
            string authorization = values.First();

            if (authorization.StartsWith("Bearer "))
            {
                string token = authorization[(authorization.LastIndexOf(' ') + 1)..];

                if (req.Headers.TryGetValues("X-UserId", out IEnumerable<string>? userIds) && Guid.TryParse(userIds.First(), out Guid userId))
                {
                    LoginInfo? loginInfo = await dbContext.GetLoginInfoByUserIdAsync(userId);

                    if (loginInfo.HasValue)
                    {
                        if (loginInfo.Value.CryptoKeys == null)
                        {
                            resp.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                            resp.StatusCode = HttpStatusCode.Unauthorized;
                            await resp.WriteStringAsync("Authentication failed.");

                            return resp;
                        }

                        using RSACryptoServiceProvider rsa = new();

                        rsa.ImportCspBlob(loginInfo.Value.CryptoKeys);
                        RefreshToken? refreshTokenObj = null;

                        try
                        {
                            string refreshTokenJson = Encoding.UTF8.GetString(rsa.Decrypt(Convert.FromBase64String(token), false));
                            refreshTokenObj = JsonSerializer.Deserialize<RefreshToken>(refreshTokenJson, JsonSerializationOptions.CamelCaseNamingOptions);
                        }
                        catch
                        {
                            resp.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                            resp.StatusCode = HttpStatusCode.Unauthorized;
                            await resp.WriteStringAsync("Invalid token.");

                            return resp;
                        }

                        if (refreshTokenObj != null)
                        {
                            DateTime now = DateTime.UtcNow;
                            Guid refreshTokenId = Guid.NewGuid();
                            bool isValidToken = refreshTokenObj.UserId == loginInfo.Value.UserId && refreshTokenObj.Id == loginInfo.Value.RefreshTokenId;
                            bool isAuthExpired = refreshTokenObj.ExpiresOn < now;

                            if (isValidToken && !isAuthExpired)
                            {
                                string accessTokenJson = JsonSerializer.Serialize(new AccessToken
                                {
                                    UserId = userId,
                                    Role = loginInfo.Value.UserRole,
                                    IssuedOn = now
                                }, JsonSerializationOptions.CamelCaseNamingOptions);

                                string refreshTokenJson = JsonSerializer.Serialize(new RefreshToken
                                {
                                    Id = refreshTokenId,
                                    UserId = userId,
                                    ExpiresOn = now.AddSeconds(Settings.RefreshTokenExpirationSpanInSecs)
                                }, JsonSerializationOptions.CamelCaseNamingOptions);

                                string accessToken = Convert.ToBase64String(rsa.Encrypt(Encoding.UTF8.GetBytes(accessTokenJson), false));
                                string refreshToken = Convert.ToBase64String(rsa.Encrypt(Encoding.UTF8.GetBytes(refreshTokenJson), false));

                                resp.Headers.Add("Content-Type", "application/json; charset=utf-8");
                                resp.StatusCode = HttpStatusCode.OK;
                                await resp.WriteStringAsync(JsonSerializer.Serialize(new
                                {
                                    AccessToken = accessToken,
                                    RefreshToken = refreshToken
                                }, JsonSerializationOptions.CamelCaseNamingOptions));

                                await dbContext.UpsertLoginAsync(new()
                                {
                                    Id = loginInfo.Value.Id,
                                    UserId = userId,
                                    CryptoKeys = loginInfo.Value.CryptoKeys,
                                    RefreshTokenId = refreshTokenId,
                                    ExpiresOn = now.AddSeconds(Settings.AccessTokenExpirationSpanInSecs),
                                    CreatedOn = now
                                });
                            }
                            else
                            {
                                resp.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                                resp.StatusCode = HttpStatusCode.Unauthorized;
                                await resp.WriteStringAsync(isAuthExpired ? "Authentication expired." : "Invalid token.");

                                if (isAuthExpired)
                                {
                                    await dbContext.DeleteLoginByUserIdAsync(userId);
                                }
                            }

                            return resp;
                        }
                    }
                }
            }
        }

        resp.Headers.Add("Content-Type", "text/plain; charset=utf-8");
        resp.StatusCode = HttpStatusCode.Unauthorized;
        await resp.WriteStringAsync("Authentication failed.");

        return resp;
    }

    [FunctionAuthorize]
    [Function("Logout")]
    public async Task<HttpResponseData> RunLogout([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        logger.LogInformation("[Logout] function processed a request.");

        HttpResponseData resp = req.CreateResponse();
        ClaimsPrincipal user = req.FunctionContext.GetHttpContext()!.User;
        resp.Headers.Add("Content-Type", "text/plain; charset=utf-8");
        resp.StatusCode = HttpStatusCode.OK;

        if (user.Identity!.IsAuthenticated)
        {
            await dbContext.DeleteLoginByUserIdAsync(Guid.Parse(user.Claims.First(c => c.Type == "userId").Value));
            await resp.WriteStringAsync($"{user.Identity.Name}");
        }

        return resp;
    }
}