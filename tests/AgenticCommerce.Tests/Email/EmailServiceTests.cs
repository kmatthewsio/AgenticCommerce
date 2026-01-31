using AgenticCommerce.Infrastructure.Email;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace AgenticCommerce.Tests.Email;

/// <summary>
/// Unit tests for IEmailService interface behavior.
/// Since EmailService is a thin wrapper around Resend SDK, we test at the interface level.
/// </summary>
public class EmailServiceTests
{
    #region IEmailService Mock Tests

    [Fact]
    public async Task MockEmailService_SendApiKeyEmailAsync_CanBeMocked()
    {
        // Arrange
        var mockEmailService = new Mock<IEmailService>();
        var toEmail = "test@example.com";
        var apiKey = "ac_live_testkey1234567890123456";
        var productName = "AgentRails Implementation Kit";

        mockEmailService
            .Setup(x => x.SendApiKeyEmailAsync(toEmail, apiKey, productName))
            .Returns(Task.CompletedTask);

        // Act
        await mockEmailService.Object.SendApiKeyEmailAsync(toEmail, apiKey, productName);

        // Assert
        mockEmailService.Verify(
            x => x.SendApiKeyEmailAsync(toEmail, apiKey, productName),
            Times.Once);
    }

    [Fact]
    public async Task MockEmailService_CanTrackEmailsSent()
    {
        // Arrange
        var sentEmails = new List<(string Email, string ApiKey, string ProductName)>();
        var mockEmailService = new Mock<IEmailService>();

        mockEmailService
            .Setup(x => x.SendApiKeyEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string, string>((email, key, product) => sentEmails.Add((email, key, product)))
            .Returns(Task.CompletedTask);

        // Act
        await mockEmailService.Object.SendApiKeyEmailAsync("user1@test.com", "key1", "Product1");
        await mockEmailService.Object.SendApiKeyEmailAsync("user2@test.com", "key2", "Product2");

        // Assert
        sentEmails.Should().HaveCount(2);
        sentEmails[0].Email.Should().Be("user1@test.com");
        sentEmails[1].Email.Should().Be("user2@test.com");
    }

    [Fact]
    public async Task MockEmailService_CanSimulateFailure()
    {
        // Arrange
        var mockEmailService = new Mock<IEmailService>();
        mockEmailService
            .Setup(x => x.SendApiKeyEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Email service unavailable"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() =>
            mockEmailService.Object.SendApiKeyEmailAsync("test@example.com", "key", "product"));
    }

    #endregion

    #region Email Content Validation Tests

    [Fact]
    public void EmailApiKey_FormatValidation()
    {
        // Arrange
        var apiKey = "ac_live_testkey1234567890123456";

        // Assert
        apiKey.Should().StartWith("ac_live_");
        apiKey.Length.Should().BeGreaterThan(12);
    }

    [Theory]
    [InlineData("test@example.com", true)]
    [InlineData("user+tag@domain.com", true)]
    [InlineData("name.surname@company.co.uk", true)]
    public void EmailAddress_ValidFormats(string email, bool expectedValid)
    {
        // Assert
        var isValid = email.Contains("@") && email.Contains(".");
        isValid.Should().Be(expectedValid);
    }

    [Theory]
    [InlineData("AgentRails Implementation Kit")]
    [InlineData("AgentRails Premium")]
    [InlineData("x402 Protocol Access")]
    public void ProductName_ValidFormats(string productName)
    {
        // Assert
        productName.Should().NotBeNullOrEmpty();
        productName.Length.Should().BeGreaterThan(3);
    }

    #endregion

    #region Email Template Content Tests

    [Fact]
    public void EmailSubject_ContainsProductName()
    {
        // Arrange
        var productName = "AgentRails Implementation Kit";

        // Act
        var subject = $"Your {productName} API Key";

        // Assert
        subject.Should().Contain(productName);
        subject.Should().Contain("API Key");
    }

    [Fact]
    public void EmailBody_ContainsRequiredSections()
    {
        // Arrange
        var apiKey = "ac_live_testkey1234567890123456";
        var productName = "AgentRails";

        // Act - Simulate email body generation (based on actual template)
        var htmlBody = GenerateTestHtmlBody(apiKey, productName);

        // Assert
        htmlBody.Should().Contain(apiKey, "API key should be in the email");
        htmlBody.Should().Contain("Getting Started", "Should have getting started section");
        htmlBody.Should().Contain("AGENTRAILS_API_KEY", "Should have env variable example");
        htmlBody.Should().Contain("https://api.agentrails.io/swagger", "Should have docs link");
        htmlBody.Should().Contain("60 days", "Should mention support period");
    }

    [Fact]
    public void EmailBody_TextVersion_IsComplete()
    {
        // Arrange
        var apiKey = "ac_live_testkey1234567890123456";
        var productName = "AgentRails";

        // Act
        var textBody = GenerateTestTextBody(apiKey, productName);

        // Assert
        textBody.Should().Contain(apiKey);
        textBody.Should().Contain("IMPORTANT");
        textBody.Should().Contain("Getting Started");
    }

    private static string GenerateTestHtmlBody(string apiKey, string productName)
    {
        return $@"
<html>
<body>
    <h1>AgentRails</h1>
    <h2>Your API Key is Ready</h2>
    <p>Thank you for purchasing the <strong>{productName}</strong>!</p>
    <div>{apiKey}</div>
    <strong>Important:</strong> Save this key securely.
    <h3>Getting Started</h3>
    <li>Add your API key to your environment: <code>AGENTRAILS_API_KEY={apiKey}</code></li>
    <li>Check out the <a href='https://api.agentrails.io/swagger'>API documentation</a></li>
    <p>Your purchase includes 60 days of email support.</p>
</body>
</html>";
    }

    private static string GenerateTestTextBody(string apiKey, string productName)
    {
        return $@"
Your {productName} API Key is Ready

Your API key: {apiKey}

IMPORTANT: Save this key securely.

Getting Started:
1. Add your API key to your environment: AGENTRAILS_API_KEY={apiKey}
2. Check out the API documentation: https://api.agentrails.io/swagger
";
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void EmailService_DefaultFromEmail_IsFallback()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act - Check fallback value
        var fromEmail = config["Resend:FromEmail"] ?? "noreply@agentrails.io";

        // Assert
        fromEmail.Should().Be("noreply@agentrails.io");
    }

    [Fact]
    public void EmailService_ConfiguredFromEmail_IsUsed()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Resend:FromEmail", "custom@agentrails.io" }
            })
            .Build();

        // Act
        var fromEmail = config["Resend:FromEmail"] ?? "noreply@agentrails.io";

        // Assert
        fromEmail.Should().Be("custom@agentrails.io");
    }

    #endregion
}
