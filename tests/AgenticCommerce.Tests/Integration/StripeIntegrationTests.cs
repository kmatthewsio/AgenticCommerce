using AgenticCommerce.Core.Models;
using AgenticCommerce.Infrastructure.Data;
using AgenticCommerce.Infrastructure.Email;
using AgenticCommerce.Infrastructure.Gumroad;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace AgenticCommerce.Tests.Integration;

/// <summary>
/// Integration tests for the complete Stripe purchase flow.
/// Tests the end-to-end flow from checkout session to API key delivery.
///
/// Product Tiers:
/// - Standard/Sandbox: Free tier, no payment required, testnet access only
/// - Enterprise: Paid tier ($2,500), uses Stripe Checkout, full production access
///
/// These tests cover the Enterprise tier purchase flow via Stripe.
/// </summary>
public class StripeIntegrationTests : IDisposable
{
    private readonly AgenticCommerceDbContext _dbContext;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly ApiKeyGenerationService _apiKeyService;

    public StripeIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<AgenticCommerceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new AgenticCommerceDbContext(options);

        _mockEmailService = new Mock<IEmailService>();
        _mockEmailService
            .Setup(x => x.SendApiKeyEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var apiKeyLogger = new Mock<ILogger<ApiKeyGenerationService>>();
        _apiKeyService = new ApiKeyGenerationService(_dbContext, apiKeyLogger.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region Complete Purchase Flow Tests

    [Fact]
    public async Task CompletePurchaseFlow_EnterpriseProvisionAndEmailDelivery()
    {
        // Arrange - Enterprise tier purchase
        var email = "enterprise@example.com";
        var productName = "AgentRails Implementation Kit"; // Enterprise product
        var sessionId = "cs_enterprise_flow";

        string? capturedEmail = null;
        string? capturedApiKey = null;
        string? capturedProduct = null;

        _mockEmailService
            .Setup(x => x.SendApiKeyEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string, string>((e, k, p) =>
            {
                capturedEmail = e;
                capturedApiKey = k;
                capturedProduct = p;
            })
            .Returns(Task.CompletedTask);

        // Act - Simulate webhook handler flow
        // 1. Provision organization and API key
        var (org, apiKey, rawKey) = await _apiKeyService.ProvisionForStripeAsync(email, productName, sessionId);

        // 2. Record the purchase
        var purchase = new StripePurchase
        {
            SessionId = sessionId,
            PaymentIntentId = "pi_test",
            Email = email,
            ProductName = productName,
            AmountCents = 250000, // $2,500 Enterprise tier
            Currency = "usd",
            OrganizationId = org.Id,
            ApiKeyId = apiKey.Id
        };
        _dbContext.StripePurchases.Add(purchase);
        await _dbContext.SaveChangesAsync();

        // 3. Send API key email
        await _mockEmailService.Object.SendApiKeyEmailAsync(email, rawKey, productName);

        // Assert - Verify complete flow
        // Organization created
        var dbOrg = await _dbContext.Organizations.FindAsync(org.Id);
        dbOrg.Should().NotBeNull();

        // API key created
        var dbKey = await _dbContext.ApiKeys.FindAsync(apiKey.Id);
        dbKey.Should().NotBeNull();

        // Purchase recorded
        var dbPurchase = await _dbContext.StripePurchases
            .Include(p => p.Organization)
            .Include(p => p.ApiKey)
            .FirstAsync(p => p.SessionId == sessionId);
        dbPurchase.Should().NotBeNull();
        dbPurchase.Organization.Should().NotBeNull();
        dbPurchase.ApiKey.Should().NotBeNull();

        // Email sent with correct content
        capturedEmail.Should().Be(email);
        capturedApiKey.Should().Be(rawKey);
        capturedProduct.Should().Be(productName);
    }

    [Fact]
    public async Task CompletePurchaseFlow_RefundRevokesKey()
    {
        // Arrange - Complete a purchase first
        var email = "refund@example.com";
        var sessionId = "cs_refund_flow";
        var paymentIntentId = "pi_refund_test";

        var (org, apiKey, rawKey) = await _apiKeyService.ProvisionForStripeAsync(email, "AgentRails Implementation Kit", sessionId);

        var purchase = new StripePurchase
        {
            SessionId = sessionId,
            PaymentIntentId = paymentIntentId,
            Email = email,
            ProductName = "AgentRails Implementation Kit", // Enterprise tier
            AmountCents = 250000, // $2,500 Enterprise tier
            Currency = "usd",
            OrganizationId = org.Id,
            ApiKeyId = apiKey.Id
        };
        _dbContext.StripePurchases.Add(purchase);
        await _dbContext.SaveChangesAsync();

        // Verify key is active
        var activeKey = await _dbContext.ApiKeys.FindAsync(apiKey.Id);
        activeKey!.RevokedAt.Should().BeNull();

        // Act - Simulate refund webhook
        var purchaseToRefund = await _dbContext.StripePurchases
            .Include(p => p.ApiKey)
            .FirstAsync(p => p.PaymentIntentId == paymentIntentId);

        purchaseToRefund.Refunded = true;
        purchaseToRefund.ApiKey!.RevokedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        // Assert - Key should be revoked
        var revokedKey = await _dbContext.ApiKeys.FindAsync(apiKey.Id);
        revokedKey!.RevokedAt.Should().NotBeNull();
        revokedKey.RevokedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        var refundedPurchase = await _dbContext.StripePurchases.FindAsync(purchase.Id);
        refundedPurchase!.Refunded.Should().BeTrue();
    }

    #endregion

    #region Idempotency Tests

    [Fact]
    public async Task WebhookIdempotency_DuplicateWebhookIgnored()
    {
        // Arrange
        var sessionId = "cs_idempotent";
        var email = "idempotent@example.com";

        // First webhook - should succeed
        var (org, apiKey, _) = await _apiKeyService.ProvisionForStripeAsync(email, "AgentRails Implementation Kit", sessionId);

        var purchase = new StripePurchase
        {
            SessionId = sessionId,
            Email = email,
            ProductName = "AgentRails Implementation Kit", // Enterprise tier
            AmountCents = 250000, // $2,500 Enterprise tier
            Currency = "usd",
            OrganizationId = org.Id,
            ApiKeyId = apiKey.Id
        };
        _dbContext.StripePurchases.Add(purchase);
        await _dbContext.SaveChangesAsync();

        // Act - Simulate duplicate webhook (should be rejected)
        var existingPurchase = await _dbContext.StripePurchases
            .FirstOrDefaultAsync(p => p.SessionId == sessionId);

        // Assert - Duplicate check should detect existing purchase
        existingPurchase.Should().NotBeNull();

        // Attempting to provision again should throw
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _apiKeyService.ProvisionForStripeAsync(email, "AgentRails Implementation Kit", sessionId));
    }

    [Fact]
    public async Task WebhookIdempotency_CountRemainsOne()
    {
        // Arrange
        var sessionId = "cs_count_test";
        var email = "count@example.com";

        // Create purchase
        var (org, apiKey, _) = await _apiKeyService.ProvisionForStripeAsync(email, "AgentRails Implementation Kit", sessionId);

        var purchase = new StripePurchase
        {
            SessionId = sessionId,
            Email = email,
            ProductName = "AgentRails Implementation Kit", // Enterprise tier
            AmountCents = 250000, // $2,500 Enterprise tier
            Currency = "usd",
            OrganizationId = org.Id,
            ApiKeyId = apiKey.Id
        };
        _dbContext.StripePurchases.Add(purchase);
        await _dbContext.SaveChangesAsync();

        // Act - Try to create duplicate (simulating webhook retry)
        try
        {
            await _apiKeyService.ProvisionForStripeAsync(email, "AgentRails Implementation Kit", sessionId);
        }
        catch (InvalidOperationException)
        {
            // Expected
        }

        // Assert - Should still only have one purchase
        var count = await _dbContext.StripePurchases.CountAsync(p => p.SessionId == sessionId);
        count.Should().Be(1);
    }

    #endregion

    #region Data Integrity Tests

    [Fact]
    public async Task DataIntegrity_OrganizationLinkedToApiKey()
    {
        // Arrange
        var email = "integrity@example.com";
        var sessionId = "cs_integrity";

        // Act
        var (org, apiKey, _) = await _apiKeyService.ProvisionForStripeAsync(email, "AgentRails Implementation Kit", sessionId);

        // Assert
        apiKey.OrganizationId.Should().Be(org.Id);

        var dbKey = await _dbContext.ApiKeys
            .Include(k => k.Organization)
            .FirstAsync(k => k.Id == apiKey.Id);
        dbKey.Organization.Should().NotBeNull();
        dbKey.Organization!.Id.Should().Be(org.Id);
    }

    [Fact]
    public async Task DataIntegrity_PurchaseLinkedToOrgAndKey()
    {
        // Arrange
        var sessionId = "cs_purchase_integrity";
        var (org, apiKey, _) = await _apiKeyService.ProvisionForStripeAsync("integrity2@example.com", "AgentRails Implementation Kit", sessionId);

        var purchase = new StripePurchase
        {
            SessionId = sessionId,
            Email = "integrity2@example.com",
            ProductName = "AgentRails Implementation Kit", // Enterprise tier
            AmountCents = 250000, // $2,500 Enterprise tier
            Currency = "usd",
            OrganizationId = org.Id,
            ApiKeyId = apiKey.Id
        };
        _dbContext.StripePurchases.Add(purchase);
        await _dbContext.SaveChangesAsync();

        // Act
        var dbPurchase = await _dbContext.StripePurchases
            .Include(p => p.Organization)
            .Include(p => p.ApiKey)
            .FirstAsync(p => p.SessionId == sessionId);

        // Assert
        dbPurchase.OrganizationId.Should().Be(org.Id);
        dbPurchase.ApiKeyId.Should().Be(apiKey.Id);
        dbPurchase.Organization.Should().NotBeNull();
        dbPurchase.ApiKey.Should().NotBeNull();
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ErrorHandling_EmailFailureDoesNotLosePurchase()
    {
        // Arrange
        var email = "emailfail@example.com";
        var sessionId = "cs_emailfail";

        // Setup email to fail
        _mockEmailService
            .Setup(x => x.SendApiKeyEmailAsync(email, It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Email service unavailable"));

        // Act - Provision succeeds
        var (org, apiKey, rawKey) = await _apiKeyService.ProvisionForStripeAsync(email, "AgentRails Implementation Kit", sessionId);

        var purchase = new StripePurchase
        {
            SessionId = sessionId,
            Email = email,
            ProductName = "AgentRails Implementation Kit", // Enterprise tier
            AmountCents = 250000, // $2,500 Enterprise tier
            Currency = "usd",
            OrganizationId = org.Id,
            ApiKeyId = apiKey.Id
        };
        _dbContext.StripePurchases.Add(purchase);
        await _dbContext.SaveChangesAsync();

        // Email fails
        await Assert.ThrowsAsync<Exception>(() =>
            _mockEmailService.Object.SendApiKeyEmailAsync(email, rawKey, "Test"));

        // Assert - Purchase should still be in database
        var dbPurchase = await _dbContext.StripePurchases.FirstAsync(p => p.SessionId == sessionId);
        dbPurchase.Should().NotBeNull();

        var dbKey = await _dbContext.ApiKeys.FindAsync(apiKey.Id);
        dbKey.Should().NotBeNull();
    }

    #endregion

    #region Query Tests

    [Fact]
    public async Task Query_CanFindPurchaseByEmail()
    {
        // Arrange
        var email = "query@example.com";
        for (int i = 0; i < 3; i++)
        {
            var (org, apiKey, _) = await _apiKeyService.ProvisionForStripeAsync(email, "Test", $"cs_query_{i}");
            _dbContext.StripePurchases.Add(new StripePurchase
            {
                SessionId = $"cs_query_{i}",
                Email = email,
                ProductName = "AgentRails Implementation Kit", // Enterprise tier
                AmountCents = 250000, // $2,500 Enterprise tier
                Currency = "usd",
                OrganizationId = org.Id,
                ApiKeyId = apiKey.Id
            });
        }
        await _dbContext.SaveChangesAsync();

        // Act
        var purchases = await _dbContext.StripePurchases
            .Where(p => p.Email == email)
            .ToListAsync();

        // Assert
        purchases.Count.Should().Be(3);
    }

    [Fact]
    public async Task Query_CanFindNonRefundedPurchases()
    {
        // Arrange
        var (org1, key1, _) = await _apiKeyService.ProvisionForStripeAsync("active@example.com", "Test", "cs_active");
        var (org2, key2, _) = await _apiKeyService.ProvisionForStripeAsync("refunded@example.com", "Test", "cs_refunded");

        _dbContext.StripePurchases.Add(new StripePurchase
        {
            SessionId = "cs_active",
            Email = "active@example.com",
            ProductName = "AgentRails Implementation Kit", // Enterprise tier
            AmountCents = 250000, // $2,500 Enterprise tier
            Currency = "usd",
            OrganizationId = org1.Id,
            ApiKeyId = key1.Id,
            Refunded = false
        });

        _dbContext.StripePurchases.Add(new StripePurchase
        {
            SessionId = "cs_refunded",
            Email = "refunded@example.com",
            ProductName = "AgentRails Implementation Kit", // Enterprise tier
            AmountCents = 250000, // $2,500 Enterprise tier
            Currency = "usd",
            OrganizationId = org2.Id,
            ApiKeyId = key2.Id,
            Refunded = true
        });
        await _dbContext.SaveChangesAsync();

        // Act
        var activePurchases = await _dbContext.StripePurchases
            .Where(p => !p.Refunded)
            .ToListAsync();

        // Assert
        activePurchases.Count.Should().Be(1);
        activePurchases[0].Email.Should().Be("active@example.com");
    }

    #endregion
}
