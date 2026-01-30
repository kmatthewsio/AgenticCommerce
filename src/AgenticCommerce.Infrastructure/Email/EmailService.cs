using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Resend;

namespace AgenticCommerce.Infrastructure.Email;

public interface IEmailService
{
    Task SendApiKeyEmailAsync(string toEmail, string apiKey, string productName);
}

public class EmailService : IEmailService
{
    private readonly IResend _resend;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly string _fromEmail;

    public EmailService(
        IResend resend,
        IConfiguration configuration,
        ILogger<EmailService> logger)
    {
        _resend = resend;
        _configuration = configuration;
        _logger = logger;
        _fromEmail = configuration["Resend:FromEmail"] ?? "noreply@agentrails.io";
    }

    public async Task SendApiKeyEmailAsync(string toEmail, string apiKey, string productName)
    {
        var subject = $"Your {productName} API Key";

        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background: linear-gradient(135deg, #6366f1 0%, #06b6d4 100%); padding: 30px; border-radius: 12px 12px 0 0;'>
        <h1 style='color: white; margin: 0; font-size: 24px;'>AgentRails</h1>
    </div>

    <div style='background: #ffffff; padding: 30px; border: 1px solid #e5e7eb; border-top: none; border-radius: 0 0 12px 12px;'>
        <h2 style='margin-top: 0;'>Your API Key is Ready</h2>

        <p>Thank you for purchasing the <strong>{productName}</strong>!</p>

        <p>Here is your API key:</p>

        <div style='background: #1e1b4b; color: #10b981; padding: 16px; border-radius: 8px; font-family: monospace; font-size: 14px; word-break: break-all;'>
            {apiKey}
        </div>

        <div style='background: #fef3c7; border-left: 4px solid #f59e0b; padding: 12px 16px; margin: 20px 0; border-radius: 0 8px 8px 0;'>
            <strong>Important:</strong> Save this key securely. It will not be shown again.
        </div>

        <h3>Getting Started</h3>
        <ol>
            <li>Add your API key to your environment: <code>AGENTRAILS_API_KEY={apiKey}</code></li>
            <li>Check out the <a href='https://api.agentrails.io/swagger' style='color: #6366f1;'>API documentation</a></li>
            <li>Explore the <a href='https://github.com/kmatthewsio/AgenticCommerce' style='color: #6366f1;'>GitHub repository</a> for implementation examples</li>
        </ol>

        <h3>Need Help?</h3>
        <p>Your purchase includes 60 days of email support. Just reply to this email with any questions.</p>

        <hr style='border: none; border-top: 1px solid #e5e7eb; margin: 24px 0;'>

        <p style='color: #6b7280; font-size: 14px;'>
            AgentRails - Payment Infrastructure for AI Agents<br>
            <a href='https://agentrails.io' style='color: #6366f1;'>agentrails.io</a>
        </p>
    </div>
</body>
</html>";

        var textBody = $@"
Your {productName} API Key is Ready

Thank you for your purchase!

Your API key: {apiKey}

IMPORTANT: Save this key securely. It will not be shown again.

Getting Started:
1. Add your API key to your environment: AGENTRAILS_API_KEY={apiKey}
2. Check out the API documentation: https://api.agentrails.io/swagger
3. Explore the GitHub repository: https://github.com/kmatthewsio/AgenticCommerce

Need Help?
Your purchase includes 60 days of email support. Just reply to this email with any questions.

--
AgentRails - Payment Infrastructure for AI Agents
https://agentrails.io
";

        try
        {
            var message = new EmailMessage
            {
                From = _fromEmail,
                To = { toEmail },
                Subject = subject,
                HtmlBody = htmlBody,
                TextBody = textBody
            };

            await _resend.EmailSendAsync(message);
            _logger.LogInformation("Sent API key email to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send API key email to {Email}", toEmail);
            throw;
        }
    }
}
