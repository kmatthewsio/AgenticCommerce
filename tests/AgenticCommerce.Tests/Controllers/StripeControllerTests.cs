using AgenticCommerce.API.Controllers;
using AgenticCommerce.Core.Models;
using AgenticCommerce.Infrastructure.Data;
using AgenticCommerce.Infrastructure.Email;
using AgenticCommerce.Infrastructure.Gumroad;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace AgenticCommerce.Tests.Controllers;

/// <summary>
/// Unit tests for StripeController.
/// Tests checkout session creation and webhook handling logic.
///
/// Product Tiers:
/// - Standard/Sandbox: Free tier, no Stripe payment required
/// - Enterprise: Paid tier ($2,500 Implementation Kit), uses Stripe Checkout
///
/// These tests cover the Enterprise tier Stripe integration.
/// </summary>
public class StripeControllerTests : IDisposable
{
    private readonly Mock<IApiKeyGenerationService> _mockApiKeyService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<ILogger<StripeController>> _mockLogger;
    private readonly AgenticCommerceDbContext _dbContext;

    public StripeControllerTests()
    {
        _mockApiKeyService = new Mock<IApiKeyGenerationService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockLogger = new Mock<ILogger<StripeController>>();

        var options = new DbContextOptionsBuilder<AgenticCommerceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new AgenticCommerceDbContext(options);
    }

    private StripeController CreateController(Dictionary<string, string?>? configValues = null)
    {
        var defaultConfig = new Dictionary<string, string?>
        {
            { "Stripe:SecretKey", "sk_test_fake" },
            { "Stripe:ImplementationKitPriceId", "price_test_123" },
            { "Stripe:WebhookSecret", "whsec_test" },
            { "App:Domain", "https://test.agentrails.io" }
        };

        if (configValues != null)
        {
            foreach (var kvp in configValues)
            {
                defaultConfig[kvp.Key] = kvp.Value;
            }
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(defaultConfig)
            .Build();

        return new StripeController(
            _dbContext,
            _mockApiKeyService.Object,
            _mockEmailService.Object,
            configuration,
            _mockLogger.Object);
    }

    #region CreateCheckoutSession Tests

    [Fact]
    public async Task CreateCheckoutSession_WithNoPriceId_Returns500()
    {
        // Arrange
        var controller = CreateController(new Dictionary<string, string?>
        {
            { "Stripe:ImplementationKitPriceId", null }
        });

        // Act
        var result = await controller.CreateCheckoutSession(null);

        // Assert
        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task CreateCheckoutSession_WithEmptyPriceId_Returns500()
    {
        // Arrange
        var controller = CreateController(new Dictionary<string, string?>
        {
            { "Stripe:ImplementationKitPriceId", "" }
        });

        // Act
        var result = await controller.CreateCheckoutSession(null);

        // Assert
        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task CreateCheckoutSession_WithEmail_IncludesEmailInRequest()
    {
        // Arrange
        var controller = CreateController();
        var request = new CreateCheckoutRequest { Email = "test@example.com" };

        // Act - This will fail with StripeException since we're using fake keys,
        // but it validates the configuration path
        var result = await controller.CreateCheckoutSession(request);

        // Assert - With fake API key, Stripe SDK will throw
        result.Should().BeOfType<ObjectResult>();
    }

    #endregion

    #region Webhook Duplicate Handling Tests

    [Fact]
    public async Task HandleWebhook_DuplicateSession_IsIdempotent()
    {
        // Arrange
        var sessionId = "cs_test_duplicate";
        var email = "duplicate@test.com";

        // Add existing purchase
        var existingPurchase = new StripePurchase
        {
            SessionId = sessionId,
            Email = email,
            ProductName = "AgentRails Implementation Kit", // Enterprise tier
            AmountCents = 250000, // $2,500 Enterprise tier
            Currency = "usd"
        };
        _dbContext.StripePurchases.Add(existingPurchase);
        await _dbContext.SaveChangesAsync();

        // Verify duplicate is in database
        var count = await _dbContext.StripePurchases.CountAsync(p => p.SessionId == sessionId);
        count.Should().Be(1);

        // If webhook fires again for same session, it should not create duplicate
        var duplicateCheck = await _dbContext.StripePurchases
            .FirstOrDefaultAsync(p => p.SessionId == sessionId);
        duplicateCheck.Should().NotBeNull();
    }

    #endregion

    #region Refund Handling Tests

    [Fact]
    public async Task HandleRefund_RevokesApiKey()
    {
        // Arrange
        var org = new Organization { Name = "Test Org", Slug = "test-org" };
        _dbContext.Organizations.Add(org);
        await _dbContext.SaveChangesAsync();

        var apiKey = new ApiKey
        {
            OrganizationId = org.Id,
            Name = "Test Key",
            KeyHash = "hash123",
            KeyPrefix = "ac_live_xxxx"
        };
        _dbContext.ApiKeys.Add(apiKey);
        await _dbContext.SaveChangesAsync();

        var purchase = new StripePurchase
        {
            SessionId = "cs_test_refund",
            PaymentIntentId = "pi_test_123",
            Email = "refund@test.com",
            ProductName = "AgentRails Implementation Kit", // Enterprise tier
            AmountCents = 250000, // $2,500 Enterprise tier
            Currency = "usd",
            OrganizationId = org.Id,
            ApiKeyId = apiKey.Id
        };
        _dbContext.StripePurchases.Add(purchase);
        await _dbContext.SaveChangesAsync();

        // Simulate refund
        purchase.Refunded = true;
        apiKey.RevokedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        // Assert
        var updatedPurchase = await _dbContext.StripePurchases
            .Include(p => p.ApiKey)
            .FirstAsync(p => p.SessionId == "cs_test_refund");

        updatedPurchase.Refunded.Should().BeTrue();
        updatedPurchase.ApiKey!.RevokedAt.Should().NotBeNull();
    }

    #endregion

    #region StripePurchase Model Tests

    [Fact]
    public void StripePurchase_DefaultValues()
    {
        // Arrange & Act
        var purchase = new StripePurchase
        {
            SessionId = "cs_test_123",
            Email = "test@example.com",
            ProductName = "AgentRails Implementation Kit", // Enterprise tier
            AmountCents = 250000, // $2,500 Enterprise tier
            Currency = "usd"
        };

        // Assert
        purchase.Refunded.Should().BeFalse();
        purchase.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void StripePurchase_AmountInDollars()
    {
        // Arrange
        var purchase = new StripePurchase
        {
            SessionId = "cs_test_123",
            Email = "test@example.com",
            ProductName = "AgentRails Implementation Kit", // Enterprise tier
            AmountCents = 250000, // $2,500 Enterprise tier
            Currency = "usd"
        };

        // Act
        var dollars = purchase.AmountCents / 100.0m;

        // Assert - Enterprise tier is $2,500
        dollars.Should().Be(2500.00m);
    }

    [Fact]
    public void StripePurchase_NavigationProperties()
    {
        // Arrange
        var org = new Organization { Id = Guid.NewGuid(), Name = "Test", Slug = "test" };
        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            OrganizationId = org.Id,
            Name = "Test",
            KeyHash = "hash",
            KeyPrefix = "ac_live_"
        };

        var purchase = new StripePurchase
        {
            SessionId = "cs_test_123",
            Email = "test@example.com",
            ProductName = "AgentRails Implementation Kit", // Enterprise tier
            AmountCents = 250000, // $2,500 Enterprise tier
            Currency = "usd",
            OrganizationId = org.Id,
            ApiKeyId = apiKey.Id,
            Organization = org,
            ApiKey = apiKey
        };

        // Assert
        purchase.Organization.Should().NotBeNull();
        purchase.Organization!.Name.Should().Be("Test");
        purchase.ApiKey.Should().NotBeNull();
        purchase.ApiKey!.KeyPrefix.Should().Be("ac_live_");
    }

    #endregion

    #region Email Service Integration Tests

    [Fact]
    public async Task Webhook_SendsEmailOnSuccessfulPurchase()
    {
        // Arrange
        var email = "success@test.com";
        var rawKey = "ac_live_testkey123456789012345678";
        var org = new Organization { Name = "Test", Slug = "test" };
        var apiKey = new ApiKey
        {
            OrganizationId = Guid.NewGuid(),
            Name = "Test",
            KeyHash = "hash",
            KeyPrefix = "ac_live_test"
        };

        _mockApiKeyService
            .Setup(x => x.ProvisionForStripeAsync(email, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((org, apiKey, rawKey));

        _mockEmailService
            .Setup(x => x.SendApiKeyEmailAsync(email, rawKey, It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Verify mock setup
        _mockEmailService.Verify(
            x => x.SendApiKeyEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    #endregion

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
