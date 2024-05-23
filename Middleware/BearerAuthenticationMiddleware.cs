using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using SnapApp.Svc.Extensions;
using SnapApp.Svc.Models;
using SnapApp.Svc.Services;

namespace SnapApp.Svc.Middleware;
internal sealed class BearerAuthenticationMiddleware(IDatabaseService dbContext) : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var requestData = await context.GetHttpRequestDataAsync();

        if (requestData!.Headers.TryGetValues("Authorization", out IEnumerable<string>? values))
        {
            string authorization = values.First();

            if (authorization.StartsWith("Bearer "))
            {
                string token = authorization[(authorization.LastIndexOf(' ') + 1)..];

                if (requestData!.Headers.TryGetValues("X-UserId", out IEnumerable<string>? userIds) && Guid.TryParse(userIds.First(), out Guid userId))
                {
                    LoginInfo? login = await dbContext.GetLoginInfoByUserIdAsync(userId);

                    if (login.HasValue)
                    {
                        if (login.Value.CryptoKeys == null)
                        {
                            HttpResponseData resp = requestData.CreateResponse(HttpStatusCode.Unauthorized);
                            await resp.WriteStringAsync("Authentication failed.");
                            context.GetInvocationResult().Value = resp;

                            return;
                        }

                        using RSACryptoServiceProvider rsa = new();

                        rsa.ImportCspBlob(login.Value.CryptoKeys);
                        AccessToken? accessToken = null;

                        try
                        {
                            string tokenJson = Encoding.UTF8.GetString(rsa.Decrypt(Convert.FromBase64String(token), false));
                            accessToken = JsonSerializer.Deserialize<AccessToken>(tokenJson, JsonSerializationOptions.CamelCaseNamingOptions);
                        }
                        catch
                        {
                            HttpResponseData resp = requestData.CreateResponse(HttpStatusCode.BadRequest);
                            await resp.WriteStringAsync("Invalid token.");
                            context.GetInvocationResult().Value = resp;

                            return;
                        }

                        if (accessToken != null)
                        {
                            FunctionAuthorizeAttribute? fnAttr = context.FunctionDefinition.GetFunctionAuthorizeAttribute();
                            bool isValidToken = accessToken.UserId == login.Value.UserId && accessToken.Role == login.Value.UserRole && accessToken.IssuedOn.Equals(login.Value.LoginCreatedOn);
                            bool isInRole = fnAttr == null || !fnAttr.Roles.Any() || fnAttr.Roles.Contains(accessToken.Role);
                            bool isAuthExpired = login.Value.LoginExpiresOn < DateTime.UtcNow;

                            if (!isValidToken || !isInRole || isAuthExpired)
                            {
                                string responseMsg;

                                if (!isValidToken)
                                {
                                    responseMsg = "Invalid token.";
                                }
                                else if (!isInRole)
                                {
                                    responseMsg = "Access Unauthorized.";
                                }
                                else
                                {
                                    responseMsg = "Authentication expired.";
                                }

                                HttpResponseData resp = requestData.CreateResponse(!isValidToken ? HttpStatusCode.BadRequest : HttpStatusCode.Unauthorized);
                                await resp.WriteStringAsync(responseMsg);
                                context.GetInvocationResult().Value = resp;

                                return;
                            }

                            ClaimsIdentity identity = new(
                                [
                                    new Claim("loginId", login.Value.Id.ToString() ?? string.Empty, typeof(int?).FullName),
                                    new Claim("userId", login.Value.UserId.ToString(), typeof(Guid).FullName),
                                    new Claim("email", login.Value.UserEmail),
                                    new Claim("company", login.Value.UserCompany ?? string.Empty),
                                    new Claim("firstName", login.Value.UserFirstName ?? string.Empty),
                                    new Claim("lastName", login.Value.UserLastName ?? string.Empty),
                                    new Claim("phone", login.Value.UserPhone),
                                    new Claim("role", login.Value.UserRole.ToString(), typeof(UserRoles).FullName),
                                    new Claim("isEmailVerified", login.Value.IsUserEmailVerified.ToString(), typeof(bool).FullName),
                                    new Claim("picture", login.Value.UserPicture ?? string.Empty)
                                ],
                                "Bearer", "email", "role"
                            );

                            context.GetHttpContext()!.User = new(identity);
                        }
                    }
                }
            }
        }

        await next(context);
    }
}