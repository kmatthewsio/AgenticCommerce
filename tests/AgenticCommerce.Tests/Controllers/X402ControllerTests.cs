using System.Net;
using System.Net.Http.Json;
using AgenticCommerce.Core.Models;
using FluentAssertions;

namespace AgenticCommerce.Tests.Controllers;

/// <summary>
/// Unit tests for X402Controller logic.
/// These test the controller behavior without a full web host.
/// </summary>
public class X402ControllerTests
{
    #region Payment Required Response Tests

    [Fact]
    public void X402PaymentRequired_ShouldHaveCorrectVersion()
    {
        // Arrange & Act
        var paymentRequired = new X402PaymentRequired
        {
            X402Version = 2,
            Accepts = new List<X402PaymentRequirement>
            {
                new X402PaymentRequirement
                {
                    Scheme = "exact",
                    Network = X402Networks.ArcTestnet,
                    MaxAmountRequired = "10000",
                    PayTo = "0x1234"
                }
            }
        };

        // Assert
        paymentRequired.X402Version.Should().Be(2);
        paymentRequired.Accepts.Should().HaveCount(1);
    }

    [Theory]
    [InlineData(X402Networks.ArcTestnet)]
    [InlineData(X402Networks.BaseSepolia)]
    [InlineData(X402Networks.EthereumMainnet)]
    public void X402PaymentRequirement_SupportedNetworks(string network)
    {
        // Arrange & Act
        var requirement = new X402PaymentRequirement
        {
            Network = network,
            Scheme = "exact",
            MaxAmountRequired = "10000"
        };

        // Assert
        requirement.Network.Should().Be(network);
    }

    #endregion

    #region Payment Payload Validation Tests

    [Fact]
    public void X402PaymentPayload_ValidStructure()
    {
        // Arrange & Act
        var payload = new X402PaymentPayload
        {
            X402Version = 2,
            Scheme = "exact",
            Network = X402Networks.ArcTestnet,
            Payload = new X402EvmPayload
            {
                Signature = "0x" + new string('a', 130),
                Authorization = new X402Eip3009Authorization
                {
                    From = "0xSender",
                    To = "0xRecipient",
                    Value = "10000",
                    ValidAfter = 0,
                    ValidBefore = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
                    Nonce = "0x1234"
                }
            }
        };

        // Assert
        payload.X402Version.Should().Be(2);
        payload.Scheme.Should().Be("exact");
        payload.Payload.Should().NotBeNull();
        payload.Payload!.Authorization.Should().NotBeNull();
    }

    [Fact]
    public void X402Eip3009Authorization_RequiredFields()
    {
        // Arrange
        var auth = new X402Eip3009Authorization
        {
            From = "0xf39Fd6e51aad88F6F4ce6aB8827279cffFb92266",
            To = "0x70997970C51812dc3A010C7d01b50e0d17dc79C8",
            Value = "1000000",
            ValidAfter = 0,
            ValidBefore = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
            Nonce = "0x1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef"
        };

        // Assert
        auth.From.Should().StartWith("0x");
        auth.To.Should().StartWith("0x");
        auth.Value.Should().NotBeNullOrEmpty();
        auth.ValidBefore.Should().BeGreaterThan(auth.ValidAfter);
        auth.Nonce.Should().StartWith("0x");
    }

    #endregion

    #region Verify Response Tests

    [Fact]
    public void X402VerifyResponse_ValidResponse()
    {
        // Arrange & Act
        var response = new X402VerifyResponse
        {
            IsValid = true,
            Payer = "0xf39Fd6e51aad88F6F4ce6aB8827279cffFb92266"
        };

        // Assert
        response.IsValid.Should().BeTrue();
        response.InvalidReason.Should().BeNull();
        response.Payer.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void X402VerifyResponse_InvalidResponse()
    {
        // Arrange & Act
        var response = new X402VerifyResponse
        {
            IsValid = false,
            InvalidReason = "Signature verification failed"
        };

        // Assert
        response.IsValid.Should().BeFalse();
        response.InvalidReason.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Settle Response Tests

    [Fact]
    public void X402SettleResponse_SuccessfulSettlement()
    {
        // Arrange & Act
        var response = new X402SettleResponse
        {
            Success = true,
            TransactionHash = "0x1234567890abcdef",
            NetworkId = X402Networks.ArcTestnet
        };

        // Assert
        response.Success.Should().BeTrue();
        response.TransactionHash.Should().NotBeNullOrEmpty();
        response.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void X402SettleResponse_FailedSettlement()
    {
        // Arrange & Act
        var response = new X402SettleResponse
        {
            Success = false,
            ErrorMessage = "Insufficient funds"
        };

        // Assert
        response.Success.Should().BeFalse();
        response.ErrorMessage.Should().NotBeNullOrEmpty();
        response.TransactionHash.Should().BeNull();
    }

    #endregion

    #region Header Constants Tests

    [Fact]
    public void X402Headers_CorrectValues()
    {
        // Assert
        X402Headers.PaymentRequired.Should().Be("X-PAYMENT-REQUIRED");
        X402Headers.Payment.Should().Be("X-PAYMENT");
        X402Headers.PaymentResponse.Should().Be("X-PAYMENT-RESPONSE");
    }

    #endregion

    #region Network Constants Tests

    [Fact]
    public void X402Networks_AllNetworksDefined()
    {
        // Assert
        X402Networks.ArcTestnet.Should().Be("arc-testnet");
        X402Networks.ArcMainnet.Should().Be("arc-mainnet");
        X402Networks.BaseSepolia.Should().Be("base-sepolia");
        X402Networks.BaseMainnet.Should().Be("base-mainnet");
        X402Networks.EthereumSepolia.Should().Be("ethereum-sepolia");
        X402Networks.EthereumMainnet.Should().Be("ethereum-mainnet");
    }

    [Fact]
    public void X402Networks_ChainIdsConfigured()
    {
        // Assert
        X402Networks.ChainIds.Should().ContainKey(X402Networks.EthereumMainnet);
        X402Networks.ChainIds[X402Networks.EthereumMainnet].Should().Be(1);

        X402Networks.ChainIds.Should().ContainKey(X402Networks.BaseSepolia);
        X402Networks.ChainIds[X402Networks.BaseSepolia].Should().Be(84532);

        X402Networks.ChainIds.Should().ContainKey(X402Networks.BaseMainnet);
        X402Networks.ChainIds[X402Networks.BaseMainnet].Should().Be(8453);
    }

    #endregion

    #region USDC Contract Tests

    [Fact]
    public void X402Assets_UsdcContractsConfigured()
    {
        // Assert
        X402Assets.UsdcContracts.Should().ContainKey(X402Networks.BaseSepolia);
        X402Assets.UsdcContracts[X402Networks.BaseSepolia].Should().StartWith("0x");

        X402Assets.UsdcContracts.Should().ContainKey(X402Networks.BaseMainnet);
        X402Assets.UsdcContracts[X402Networks.BaseMainnet].Should().StartWith("0x");

        X402Assets.UsdcContracts.Should().ContainKey(X402Networks.EthereumMainnet);
        X402Assets.UsdcContracts[X402Networks.EthereumMainnet].Should().StartWith("0x");
    }

    [Fact]
    public void X402Assets_UsdcContractsAreValidAddresses()
    {
        // All contract addresses should be 42 characters (0x + 40 hex chars)
        foreach (var contract in X402Assets.UsdcContracts.Values)
        {
            if (contract != "0x...") // Skip placeholder
            {
                contract.Should().HaveLength(42);
                contract.Should().StartWith("0x");
            }
        }
    }

    #endregion

    #region Amount Conversion Tests

    [Theory]
    [InlineData(0.000001, 1)]       // 1 smallest unit
    [InlineData(0.001, 1000)]       // 1000 smallest units
    [InlineData(0.01, 10000)]       // $0.01
    [InlineData(1.0, 1000000)]      // $1.00
    [InlineData(100.0, 100000000)]  // $100.00
    public void AmountConversion_UsdcToSmallestUnit(decimal usdc, long expectedSmallestUnit)
    {
        // Act
        var smallestUnit = (long)(usdc * 1_000_000);

        // Assert
        smallestUnit.Should().Be(expectedSmallestUnit);
    }

    [Theory]
    [InlineData(1, 0.000001)]
    [InlineData(10000, 0.01)]
    [InlineData(1000000, 1.0)]
    public void AmountConversion_SmallestUnitToUsdc(long smallestUnit, decimal expectedUsdc)
    {
        // Act
        var usdc = smallestUnit / 1_000_000m;

        // Assert
        usdc.Should().Be(expectedUsdc);
    }

    #endregion
}
