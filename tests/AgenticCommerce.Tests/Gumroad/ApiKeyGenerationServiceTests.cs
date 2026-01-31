using AgenticCommerce.Core.Models;
using AgenticCommerce.Infrastructure.Data;
using AgenticCommerce.Infrastructure.Gumroad;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace AgenticCommerce.Tests.Gumroad;

/// <summary>
/// Unit tests for ApiKeyGenerationService.
/// Tests API key generation, organization provisioning, and duplicate handling.
///
/// Product Tiers:
/// - Standard/Sandbox: Free tier, no API key purchase required
/// - Enterprise: Paid tier ($2,500), API key provisioned after Stripe/Gumroad purchase
///
/// These tests cover the Enterprise tier API key provisioning.
/// </summary>
public class ApiKeyGenerationServiceTests : IDisposable
{
    private readonly AgenticCommerceDbContext _dbContext;
    private readonly Mock<ILogger<ApiKeyGenerationService>> _mockLogger;
    private readonly ApiKeyGenerationService _service;

    public ApiKeyGenerationServiceTests()
    {
        var options = new DbContextOptionsBuilder<AgenticCommerceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new AgenticCommerceDbContext(options);
        _mockLogger = new Mock<ILogger<ApiKeyGenerationService>>();
        _service = new ApiKeyGenerationService(_dbContext, _mockLogger.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region ProvisionForStripeAsync Tests

    [Fact]
    public async Task ProvisionForStripeAsync_CreatesOrganization()
    {
        // Arrange
        var email = "newuser@example.com";
        var productName = "AgentRails Implementation Kit";
        var sessionId = "cs_test_123";

        // Act
        var (org, apiKey, rawKey) = await _service.ProvisionForStripeAsync(email, productName, sessionId);

        // Assert
        org.Should().NotBeNull();
        org.Name.Should().Contain(productName);
        org.Name.Should().Contain(email);
        org.Slug.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ProvisionForStripeAsync_CreatesApiKey()
    {
        // Arrange
        var email = "apikey@example.com";
        var sessionId = "cs_test_apikey";

        // Act
        var (org, apiKey, rawKey) = await _service.ProvisionForStripeAsync(email, "AgentRails Implementation Kit", sessionId);

        // Assert
        apiKey.Should().NotBeNull();
        apiKey.OrganizationId.Should().Be(org.Id);
        apiKey.KeyPrefix.Should().StartWith("ac_live_");
        apiKey.KeyHash.Should().NotBeNullOrEmpty();
        apiKey.Name.Should().Contain("Stripe Purchase");
        apiKey.Name.Should().Contain(sessionId);
    }

    [Fact]
    public async Task ProvisionForStripeAsync_ReturnsRawKey()
    {
        // Arrange
        var email = "rawkey@example.com";
        var sessionId = "cs_test_rawkey";

        // Act
        var (org, apiKey, rawKey) = await _service.ProvisionForStripeAsync(email, "AgentRails Implementation Kit", sessionId);

        // Assert
        rawKey.Should().NotBeNullOrEmpty();
        rawKey.Should().StartWith("ac_live_");
        rawKey.Length.Should().BeGreaterThan(12);
        rawKey.Should().StartWith(apiKey.KeyPrefix);
    }

    [Fact]
    public async Task ProvisionForStripeAsync_HashesKeySecurely()
    {
        // Arrange
        var email = "hash@example.com";
        var sessionId = "cs_test_hash";

        // Act
        var (org, apiKey, rawKey) = await _service.ProvisionForStripeAsync(email, "AgentRails Implementation Kit", sessionId);

        // Assert
        apiKey.KeyHash.Should().NotBe(rawKey);
        apiKey.KeyHash.Should().NotContain("ac_live_");
        // Hash should be base64 encoded SHA256
        apiKey.KeyHash.Length.Should().Be(44); // Base64 encoded 32 bytes = 44 chars
    }

    [Fact]
    public async Task ProvisionForStripeAsync_ThrowsOnDuplicateSession()
    {
        // Arrange
        var email = "duplicate@example.com";
        var sessionId = "cs_test_duplicate";

        // First provision
        var (org1, apiKey1, rawKey1) = await _service.ProvisionForStripeAsync(email, "AgentRails Implementation Kit", sessionId);

        // Link to a purchase record (simulating the controller behavior)
        var purchase = new StripePurchase
        {
            SessionId = sessionId,
            Email = email,
            ProductName = "AgentRails Implementation Kit", // Enterprise tier
            AmountCents = 4900,
            Currency = "usd",
            OrganizationId = org1.Id,
            ApiKeyId = apiKey1.Id
        };
        _dbContext.StripePurchases.Add(purchase);
        await _dbContext.SaveChangesAsync();

        // Act & Assert - Second provision should throw
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ProvisionForStripeAsync(email, "AgentRails Implementation Kit", sessionId));
    }

    [Fact]
    public async Task ProvisionForStripeAsync_GeneratesUniqueSlug()
    {
        // Arrange
        var email = "sameuser@example.com";

        // Act - Create two provisions with same email prefix
        var (org1, _, _) = await _service.ProvisionForStripeAsync(email, "Test1", "cs_1");
        var (org2, _, _) = await _service.ProvisionForStripeAsync(email, "Test2", "cs_2");

        // Assert - Slugs should be different due to random suffix
        org1.Slug.Should().NotBe(org2.Slug);
        org1.Slug.Should().StartWith("sameuser-");
        org2.Slug.Should().StartWith("sameuser-");
    }

    [Fact]
    public async Task ProvisionForStripeAsync_GeneratesUniqueApiKeys()
    {
        // Arrange & Act
        var results = new List<(ApiKey apiKey, string rawKey)>();
        for (int i = 0; i < 5; i++)
        {
            var (org, apiKey, rawKey) = await _service.ProvisionForStripeAsync(
                $"unique{i}@example.com", "AgentRails Implementation Kit", $"cs_unique_{i}");
            results.Add((apiKey, rawKey));
        }

        // Assert - All keys should be unique
        var hashes = results.Select(r => r.apiKey.KeyHash).ToList();
        var rawKeys = results.Select(r => r.rawKey).ToList();

        hashes.Distinct().Count().Should().Be(5);
        rawKeys.Distinct().Count().Should().Be(5);
    }

    #endregion

    #region ProvisionForPurchaseAsync Tests (Gumroad)

    [Fact]
    public async Task ProvisionForPurchaseAsync_CreatesOrganizationAndKey()
    {
        // Arrange
        var email = "gumroad@example.com";
        var saleId = "sale_123";

        // Act
        var (org, apiKey, rawKey) = await _service.ProvisionForPurchaseAsync(email, "AgentRails Implementation Kit", saleId);

        // Assert
        org.Should().NotBeNull();
        apiKey.Should().NotBeNull();
        rawKey.Should().StartWith("ac_live_");
        apiKey.Name.Should().Contain("Gumroad Purchase");
    }

    [Fact]
    public async Task ProvisionForPurchaseAsync_ThrowsOnDuplicateSale()
    {
        // Arrange
        var email = "gumroad-dup@example.com";
        var saleId = "sale_duplicate";

        // First provision
        var (org1, apiKey1, rawKey1) = await _service.ProvisionForPurchaseAsync(email, "AgentRails Implementation Kit", saleId);

        // Link to a purchase record
        var purchase = new GumroadPurchase
        {
            SaleId = saleId,
            ProductId = "prod_test",
            Email = email,
            ProductName = "AgentRails Implementation Kit", // Enterprise tier
            PriceCents = 250000, // $2,500 Enterprise tier
            Currency = "usd",
            OrganizationId = org1.Id,
            ApiKeyId = apiKey1.Id
        };
        _dbContext.GumroadPurchases.Add(purchase);
        await _dbContext.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ProvisionForPurchaseAsync(email, "AgentRails Implementation Kit", saleId));
    }

    #endregion

    #region GetApiKeyByStripeSessionIdAsync Tests

    [Fact]
    public async Task GetApiKeyByStripeSessionIdAsync_ReturnsNullForUnknownSession()
    {
        // Act
        var result = await _service.GetApiKeyByStripeSessionIdAsync("cs_unknown");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetApiKeyByStripeSessionIdAsync_ReturnsApiKeyForKnownSession()
    {
        // Arrange
        var sessionId = "cs_known";
        var (org, apiKey, _) = await _service.ProvisionForStripeAsync("known@example.com", "AgentRails Implementation Kit", sessionId);

        var purchase = new StripePurchase
        {
            SessionId = sessionId,
            Email = "known@example.com",
            ProductName = "AgentRails Implementation Kit", // Enterprise tier
            AmountCents = 4900,
            Currency = "usd",
            OrganizationId = org.Id,
            ApiKeyId = apiKey.Id
        };
        _dbContext.StripePurchases.Add(purchase);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetApiKeyByStripeSessionIdAsync(sessionId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(apiKey.Id);
    }

    #endregion

    #region GetApiKeyBySaleIdAsync Tests

    [Fact]
    public async Task GetApiKeyBySaleIdAsync_ReturnsNullForUnknownSale()
    {
        // Act
        var result = await _service.GetApiKeyBySaleIdAsync("sale_unknown");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetApiKeyBySaleIdAsync_ReturnsApiKeyForKnownSale()
    {
        // Arrange
        var saleId = "sale_known";
        var (org, apiKey, _) = await _service.ProvisionForPurchaseAsync("known@example.com", "AgentRails Implementation Kit", saleId);

        var purchase = new GumroadPurchase
        {
            SaleId = saleId,
            ProductId = "prod_known",
            Email = "known@example.com",
            ProductName = "AgentRails Implementation Kit", // Enterprise tier
            PriceCents = 250000, // $2,500 Enterprise tier
            Currency = "usd",
            OrganizationId = org.Id,
            ApiKeyId = apiKey.Id
        };
        _dbContext.GumroadPurchases.Add(purchase);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetApiKeyBySaleIdAsync(saleId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(apiKey.Id);
    }

    #endregion

    #region Slug Generation Tests

    [Theory]
    [InlineData("simple@example.com", "simple")]
    [InlineData("User.Name@example.com", "user-name")]
    [InlineData("user+tag@example.com", "user-tag")]
    [InlineData("user_name@example.com", "user-name")]
    [InlineData("UPPERCASE@example.com", "uppercase")]
    public async Task ProvisionForStripeAsync_GeneratesValidSlugFromEmail(string email, string expectedPrefix)
    {
        // Act
        var (org, _, _) = await _service.ProvisionForStripeAsync(email, "AgentRails Implementation Kit", $"cs_{Guid.NewGuid()}");

        // Assert
        org.Slug.Should().StartWith(expectedPrefix + "-");
        org.Slug.Should().MatchRegex(@"^[a-z0-9-]+$");
    }

    [Fact]
    public async Task ProvisionForStripeAsync_HandlesSpecialCharactersInEmail()
    {
        // Arrange
        var email = "user!#$%@example.com";

        // Act
        var (org, _, _) = await _service.ProvisionForStripeAsync(email, "AgentRails Implementation Kit", "cs_special");

        // Assert
        org.Slug.Should().NotContain("!");
        org.Slug.Should().NotContain("#");
        org.Slug.Should().NotContain("$");
        org.Slug.Should().NotContain("%");
        org.Slug.Should().MatchRegex(@"^[a-z0-9-]+$");
    }

    #endregion

    #region API Key Format Tests

    [Fact]
    public async Task GeneratedApiKey_HasCorrectFormat()
    {
        // Act
        var (_, apiKey, rawKey) = await _service.ProvisionForStripeAsync(
            "format@example.com", "AgentRails Implementation Kit", "cs_format");

        // Assert
        // Raw key format: ac_live_xxxxxxxxxxxxxxxxxxxxxxxxxx (8 + 32 = 40 chars)
        rawKey.Should().StartWith("ac_live_");
        rawKey.Length.Should().Be(40);

        // Key prefix should be first 12 chars of the raw key
        apiKey.KeyPrefix.Should().Be(rawKey[..12]);
        apiKey.KeyPrefix.Should().StartWith("ac_live_");
        apiKey.KeyPrefix.Length.Should().Be(12);
    }

    [Fact]
    public async Task GeneratedApiKey_IsBase64UrlSafe()
    {
        // Act - Generate multiple keys to check randomness
        for (int i = 0; i < 10; i++)
        {
            var (_, _, rawKey) = await _service.ProvisionForStripeAsync(
                $"safe{i}@example.com", "AgentRails Implementation Kit", $"cs_safe_{i}");

            // Assert - Key should not contain problematic base64 characters
            var keyPart = rawKey[8..]; // Skip "ac_live_" prefix
            keyPart.Should().NotContain("+");
            keyPart.Should().NotContain("/");
            keyPart.Should().NotContain("=");
        }
    }

    #endregion

    #region Database Persistence Tests

    [Fact]
    public async Task ProvisionForStripeAsync_PersistsOrganizationToDatabase()
    {
        // Act
        var (org, _, _) = await _service.ProvisionForStripeAsync(
            "persist@example.com", "AgentRails Implementation Kit", "cs_persist");

        // Assert
        var dbOrg = await _dbContext.Organizations.FindAsync(org.Id);
        dbOrg.Should().NotBeNull();
        dbOrg!.Name.Should().Be(org.Name);
    }

    [Fact]
    public async Task ProvisionForStripeAsync_PersistsApiKeyToDatabase()
    {
        // Act
        var (_, apiKey, _) = await _service.ProvisionForStripeAsync(
            "persistkey@example.com", "AgentRails Implementation Kit", "cs_persistkey");

        // Assert
        var dbKey = await _dbContext.ApiKeys.FindAsync(apiKey.Id);
        dbKey.Should().NotBeNull();
        dbKey!.KeyHash.Should().Be(apiKey.KeyHash);
        dbKey.KeyPrefix.Should().Be(apiKey.KeyPrefix);
    }

    #endregion
}
