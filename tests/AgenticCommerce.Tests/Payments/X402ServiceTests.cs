using System.Text;
using System.Text.Json;
using AgenticCommerce.Core.Interfaces;
using AgenticCommerce.Core.Models;
using AgenticCommerce.Infrastructure.Payments;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace AgenticCommerce.Tests.Payments;

public class X402ServiceTests
{
    private readonly Mock<IArcClient> _arcClientMock;
    private readonly Mock<IEip3009SignatureVerifier> _signatureVerifierMock;
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<ILogger<X402Service>> _loggerMock;
    private readonly X402Service _service;

    private const string TestWalletAddress = "0x6255d8dd3f84ec460fc8b07db58ab06384a2f487";

    public X402ServiceTests()
    {
        _arcClientMock = new Mock<IArcClient>();
        _signatureVerifierMock = new Mock<IEip3009SignatureVerifier>();
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _loggerMock = new Mock<ILogger<X402Service>>();

        _arcClientMock.Setup(x => x.GetAddress()).Returns(TestWalletAddress);

        // Setup scope factory to return a mock scope (for database operations)
        var scopeMock = new Mock<IServiceScope>();
        var serviceProviderMock = new Mock<IServiceProvider>();
        scopeMock.Setup(x => x.ServiceProvider).Returns(serviceProviderMock.Object);
        _scopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);

        _service = new X402Service(
            _arcClientMock.Object,
            _loggerMock.Object,
            _scopeFactoryMock.Object,
            _signatureVerifierMock.Object);
    }

    #region CreatePaymentRequired Tests

    [Fact]
    public void CreatePaymentRequired_ValidInput_ReturnsCorrectRequirement()
    {
        // Arrange
        var resource = "/api/data";
        var amount = 0.01m;
        var description = "Test API call";

        // Act
        var result = _service.CreatePaymentRequired(resource, amount, description);

        // Assert
        result.X402Version.Should().Be(2);
        result.Accepts.Should().HaveCount(1);

        var requirement = result.Accepts[0];
        requirement.Scheme.Should().Be("exact");
        requirement.Network.Should().Be(X402Networks.ArcTestnet);
        requirement.MaxAmountRequired.Should().Be("10000"); // 0.01 * 1_000_000
        requirement.Resource.Should().Be(resource);
        requirement.Description.Should().Be(description);
        requirement.PayTo.Should().Be(TestWalletAddress);
    }

    [Fact]
    public void CreatePaymentRequired_CustomNetwork_UsesSpecifiedNetwork()
    {
        // Arrange
        var network = X402Networks.BaseSepolia;

        // Act
        var result = _service.CreatePaymentRequired("/api/test", 1.0m, "Test", network);

        // Assert
        result.Accepts[0].Network.Should().Be(network);
    }

    [Theory]
    [InlineData(0.001, "1000")]
    [InlineData(0.01, "10000")]
    [InlineData(1.0, "1000000")]
    [InlineData(100.0, "100000000")]
    public void CreatePaymentRequired_VariousAmounts_ConvertsToSmallestUnit(decimal usdc, string expected)
    {
        // Act
        var result = _service.CreatePaymentRequired("/api/test", usdc, "Test");

        // Assert
        result.Accepts[0].MaxAmountRequired.Should().Be(expected);
    }

    [Fact]
    public void CreatePaymentRequired_SetsExpirationTime()
    {
        // Act
        var before = DateTimeOffset.UtcNow.AddMinutes(4).ToUnixTimeSeconds();
        var result = _service.CreatePaymentRequired("/api/test", 0.01m, "Test");
        var after = DateTimeOffset.UtcNow.AddMinutes(6).ToUnixTimeSeconds();

        // Assert
        var expiresAt = result.Accepts[0].Extra?.ExpiresAt;
        expiresAt.Should().NotBeNull();
        expiresAt.Should().BeGreaterThan(before);
        expiresAt.Should().BeLessThan(after);
    }

    #endregion

    #region Encode/Decode Tests

    [Fact]
    public void EncodePaymentRequired_ReturnsValidBase64()
    {
        // Arrange
        var paymentRequired = _service.CreatePaymentRequired("/api/test", 0.01m, "Test");

        // Act
        var encoded = _service.EncodePaymentRequired(paymentRequired);

        // Assert
        encoded.Should().NotBeNullOrEmpty();

        // Should be valid Base64
        var action = () => Convert.FromBase64String(encoded);
        action.Should().NotThrow();
    }

    [Fact]
    public void EncodePaymentRequired_ContainsCorrectJson()
    {
        // Arrange
        var paymentRequired = _service.CreatePaymentRequired("/api/test", 0.01m, "Test");

        // Act
        var encoded = _service.EncodePaymentRequired(paymentRequired);
        var json = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));

        // Assert
        json.Should().Contain("\"x402Version\":2");
        json.Should().Contain("\"scheme\":\"exact\"");
        json.Should().Contain("\"maxAmountRequired\":\"10000\"");
    }

    [Fact]
    public void DecodePaymentPayload_ValidBase64_ReturnsPayload()
    {
        // Arrange
        var payload = new X402PaymentPayload
        {
            X402Version = 2,
            Scheme = "exact",
            Network = X402Networks.ArcTestnet,
            Payload = new X402EvmPayload
            {
                Signature = "0x1234",
                Authorization = new X402Eip3009Authorization
                {
                    From = "0xSender",
                    To = "0xRecipient",
                    Value = "10000",
                    ValidAfter = 0,
                    ValidBefore = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
                    Nonce = "0x1234567890abcdef"
                }
            }
        };
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

        // Act
        var result = _service.DecodePaymentPayload(base64);

        // Assert
        result.Should().NotBeNull();
        result!.X402Version.Should().Be(2);
        result.Scheme.Should().Be("exact");
        result.Payload?.Authorization?.From.Should().Be("0xSender");
    }

    [Fact]
    public void DecodePaymentPayload_InvalidBase64_ReturnsNull()
    {
        // Act
        var result = _service.DecodePaymentPayload("not-valid-base64!!!");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void DecodePaymentPayload_InvalidJson_ReturnsNull()
    {
        // Arrange
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("not valid json"));

        // Act
        var result = _service.DecodePaymentPayload(base64);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region VerifyPaymentAsync Tests

    [Fact]
    public async Task VerifyPaymentAsync_ValidPayload_ReturnsValid()
    {
        // Arrange
        var requirement = CreateTestRequirement();
        var payload = CreateTestPayload();

        _signatureVerifierMock
            .Setup(x => x.Verify(
                It.IsAny<X402Eip3009Authorization>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(new Eip3009VerificationResult
            {
                IsValid = true,
                RecoveredAddress = payload.Payload!.Authorization!.From
            });

        // Act
        var result = await _service.VerifyPaymentAsync(payload, requirement);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Payer.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task VerifyPaymentAsync_SchemeMismatch_ReturnsInvalid()
    {
        // Arrange
        var requirement = CreateTestRequirement();
        var payload = CreateTestPayload();
        payload.Scheme = "upto"; // Mismatch

        // Act
        var result = await _service.VerifyPaymentAsync(payload, requirement);

        // Assert
        result.IsValid.Should().BeFalse();
        result.InvalidReason.Should().Contain("Scheme mismatch");
    }

    [Fact]
    public async Task VerifyPaymentAsync_NetworkMismatch_ReturnsInvalid()
    {
        // Arrange
        var requirement = CreateTestRequirement();
        var payload = CreateTestPayload();
        payload.Network = X402Networks.EthereumMainnet; // Mismatch

        // Act
        var result = await _service.VerifyPaymentAsync(payload, requirement);

        // Assert
        result.IsValid.Should().BeFalse();
        result.InvalidReason.Should().Contain("Network mismatch");
    }

    [Fact]
    public async Task VerifyPaymentAsync_MissingAuthorization_ReturnsInvalid()
    {
        // Arrange
        var requirement = CreateTestRequirement();
        var payload = CreateTestPayload();
        payload.Payload!.Authorization = null;

        // Act
        var result = await _service.VerifyPaymentAsync(payload, requirement);

        // Assert
        result.IsValid.Should().BeFalse();
        result.InvalidReason.Should().Contain("Missing authorization");
    }

    [Fact]
    public async Task VerifyPaymentAsync_RecipientMismatch_ReturnsInvalid()
    {
        // Arrange
        var requirement = CreateTestRequirement();
        var payload = CreateTestPayload();
        payload.Payload!.Authorization!.To = "0xWrongAddress";

        // Act
        var result = await _service.VerifyPaymentAsync(payload, requirement);

        // Assert
        result.IsValid.Should().BeFalse();
        result.InvalidReason.Should().Contain("Recipient mismatch");
    }

    [Fact]
    public async Task VerifyPaymentAsync_InsufficientAmount_ReturnsInvalid()
    {
        // Arrange
        var requirement = CreateTestRequirement();
        var payload = CreateTestPayload();
        payload.Payload!.Authorization!.Value = "100"; // Less than required 10000

        // Act
        var result = await _service.VerifyPaymentAsync(payload, requirement);

        // Assert
        result.IsValid.Should().BeFalse();
        result.InvalidReason.Should().Contain("Insufficient amount");
    }

    [Fact]
    public async Task VerifyPaymentAsync_ExpiredAuthorization_ReturnsInvalid()
    {
        // Arrange
        var requirement = CreateTestRequirement();
        var payload = CreateTestPayload();
        payload.Payload!.Authorization!.ValidBefore = DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds();

        // Act
        var result = await _service.VerifyPaymentAsync(payload, requirement);

        // Assert
        result.IsValid.Should().BeFalse();
        result.InvalidReason.Should().Contain("expired");
    }

    [Fact]
    public async Task VerifyPaymentAsync_NotYetValid_ReturnsInvalid()
    {
        // Arrange
        var requirement = CreateTestRequirement();
        var payload = CreateTestPayload();
        payload.Payload!.Authorization!.ValidAfter = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();

        // Act
        var result = await _service.VerifyPaymentAsync(payload, requirement);

        // Assert
        result.IsValid.Should().BeFalse();
        result.InvalidReason.Should().Contain("not yet valid");
    }

    [Fact]
    public async Task VerifyPaymentAsync_MissingSignature_ReturnsInvalid()
    {
        // Arrange
        var requirement = CreateTestRequirement();
        var payload = CreateTestPayload();
        payload.Payload!.Signature = "";

        // Act
        var result = await _service.VerifyPaymentAsync(payload, requirement);

        // Assert
        result.IsValid.Should().BeFalse();
        result.InvalidReason.Should().Contain("Missing signature");
    }

    [Fact]
    public async Task VerifyPaymentAsync_InvalidSignature_ReturnsInvalid()
    {
        // Arrange
        var requirement = CreateTestRequirement();
        var payload = CreateTestPayload();

        _signatureVerifierMock
            .Setup(x => x.Verify(
                It.IsAny<X402Eip3009Authorization>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(new Eip3009VerificationResult
            {
                IsValid = false,
                ErrorMessage = "Signature does not match"
            });

        // Act
        var result = await _service.VerifyPaymentAsync(payload, requirement);

        // Assert
        result.IsValid.Should().BeFalse();
        result.InvalidReason.Should().Contain("Signature");
    }

    #endregion

    #region GetPayToAddress Tests

    [Fact]
    public void GetPayToAddress_ReturnsArcClientAddress()
    {
        // Act
        var result = _service.GetPayToAddress();

        // Assert
        result.Should().Be(TestWalletAddress);
    }

    #endregion

    #region Helper Methods

    private X402PaymentRequirement CreateTestRequirement()
    {
        return new X402PaymentRequirement
        {
            Scheme = "exact",
            Network = X402Networks.ArcTestnet,
            MaxAmountRequired = "10000",
            Resource = "/api/test",
            Description = "Test",
            PayTo = TestWalletAddress,
            Asset = "0x036CbD53842c5426634e7929541eC2318f3dCF7e"
        };
    }

    private X402PaymentPayload CreateTestPayload()
    {
        return new X402PaymentPayload
        {
            X402Version = 2,
            Scheme = "exact",
            Network = X402Networks.ArcTestnet,
            Payload = new X402EvmPayload
            {
                Signature = "0x" + new string('a', 130), // 65 bytes
                Authorization = new X402Eip3009Authorization
                {
                    From = "0xf39Fd6e51aad88F6F4ce6aB8827279cffFb92266",
                    To = TestWalletAddress,
                    Value = "10000",
                    ValidAfter = 0,
                    ValidBefore = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
                    Nonce = "0x1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef"
                }
            }
        };
    }

    #endregion
}
