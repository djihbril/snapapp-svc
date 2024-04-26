
using System.Diagnostics.CodeAnalysis;
using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Logging;

namespace SnapApp.Svc.Services;

public interface IEmailCommunicationService
{
    Task<bool> SendAsync(string sender, string recipient, string subject, string htmlContent, [AllowNull] string textContent = null);
}

public class EmailService(ILogger<EmailService> logger, EmailClient emailClient) : IEmailCommunicationService
{
    public async Task<bool> SendAsync(string sender, string recipient, string subject, string htmlContent, [AllowNull] string textContent = null)
    {
        try
        {
            EmailSendOperation emailSendOp = await emailClient.SendAsync(WaitUntil.Completed, sender, recipient, subject, htmlContent, textContent);

            return emailSendOp.Value.Status == EmailSendStatus.Succeeded;
        }
        catch (RequestFailedException ex)
        {
            logger.LogError(ex, "Error while sending email to {Email}.", sender);

            return false;
        }
    }
}