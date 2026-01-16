using System.Numerics;
using AgenticCommerce.Core.Models;
using AgenticCommerce.Infrastructure.Payments;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Nethereum.Signer;
using Nethereum.Util;

namespace AgenticCommerce.Tests.Payments;

public class Eip3009SignatureVerifierTests
{
    private readonly IEip3009SignatureVerifier _verifier;
    private readonly Mock<ILogger<Eip3009SignatureVerifier>> _loggerMock;

    // Well-known test private key (Hardhat account #0)
    // NEVER use this in production - it's publicly known
    private const string TestPrivateKey = "ac0974bec39a17e36ba4a6b4d238ff944bacb478cbed5efcae784d7bf4f2ff80";
    private const string TestAddress = "0xf39Fd6e51aad88F6F4ce6aB8827279cffFb92266";

    // Test USDC contract address (Base Sepolia)
    private const string TestUsdcContract = "0x036CbD53842c5426634e7929541eC2318f3dCF7e";
    private const string TestNetwork = X402Networks.BaseSepolia;

    public Eip3009SignatureVerifierTests()
    {
        _loggerMock = new Mock<ILogger<Eip3009SignatureVerifier>>();
        _verifier = new Eip3009SignatureVerifier(_loggerMock.Object);
    }

    [Fact]
    public void Verify_ValidSignature_ReturnsValid()
    {
        // Arrange
        var authorization = new X402Eip3009Authorization
        {
            From = TestAddress,
            To = "0x70997970C51812dc3A010C7d01b50e0d17dc79C8",
            Value = "1000000", // 1 USDC
            ValidAfter = 0,
            ValidBefore = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
            Nonce = "0x" + Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N").Substring(0, 32)
        };

        var signature = SignEip3009Authorization(authorization, TestPrivateKey, TestNetwork, TestUsdcContract);

        // Act
        var result = _verifier.Verify(authorization, signature, TestNetwork, TestUsdcContract);

        // Assert
        result.IsValid.Should().BeTrue();
        result.RecoveredAddress.Should().BeEquivalentTo(TestAddress);
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Verify_WrongFromAddress_ReturnsInvalid()
    {
        // Arrange
        var authorization = new X402Eip3009Authorization
        {
            From = "0x70997970C51812dc3A010C7d01b50e0d17dc79C8", // Wrong address
            To = "0x3C44CdDdB6a900fa2b585dd299e03d12FA4293BC",
            Value = "1000000",
            ValidAfter = 0,
            ValidBefore = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
            Nonce = "0x" + Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N").Substring(0, 32)
        };

        // Sign with TestPrivateKey but claim it's from a different address
        var signature = SignEip3009Authorization(authorization, TestPrivateKey, TestNetwork, TestUsdcContract);

        // Act
        var result = _verifier.Verify(authorization, signature, TestNetwork, TestUsdcContract);

        // Assert
        result.IsValid.Should().BeFalse();
        result.RecoveredAddress.Should().BeEquivalentTo(TestAddress); // Recovers actual signer
        result.ErrorMessage.Should().Contain("does not match");
    }

    [Fact]
    public void Verify_TamperedSignature_ReturnsInvalid()
    {
        // Arrange
        var authorization = new X402Eip3009Authorization
        {
            From = TestAddress,
            To = "0x70997970C51812dc3A010C7d01b50e0d17dc79C8",
            Value = "1000000",
            ValidAfter = 0,
            ValidBefore = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
            Nonce = "0x" + Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N").Substring(0, 32)
        };

        var signature = SignEip3009Authorization(authorization, TestPrivateKey, TestNetwork, TestUsdcContract);

        // Tamper with the signature (flip a byte)
        var tamperedSignature = signature.Substring(0, 10) + "ff" + signature.Substring(12);

        // Act
        var result = _verifier.Verify(authorization, tamperedSignature, TestNetwork, TestUsdcContract);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Verify_InvalidSignatureFormat_ReturnsInvalid()
    {
        // Arrange
        var authorization = new X402Eip3009Authorization
        {
            From = TestAddress,
            To = "0x70997970C51812dc3A010C7d01b50e0d17dc79C8",
            Value = "1000000",
            ValidAfter = 0,
            ValidBefore = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
            Nonce = "0x1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef"
        };

        // Act
        var result = _verifier.Verify(authorization, "not-a-valid-signature", TestNetwork, TestUsdcContract);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid signature format");
    }

    [Fact]
    public void Verify_EmptySignature_ReturnsInvalid()
    {
        // Arrange
        var authorization = new X402Eip3009Authorization
        {
            From = TestAddress,
            To = "0x70997970C51812dc3A010C7d01b50e0d17dc79C8",
            Value = "1000000",
            ValidAfter = 0,
            ValidBefore = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
            Nonce = "0x1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef"
        };

        // Act
        var result = _verifier.Verify(authorization, "", TestNetwork, TestUsdcContract);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Verify_WrongLengthSignature_ReturnsInvalid()
    {
        // Arrange
        var authorization = new X402Eip3009Authorization
        {
            From = TestAddress,
            To = "0x70997970C51812dc3A010C7d01b50e0d17dc79C8",
            Value = "1000000",
            ValidAfter = 0,
            ValidBefore = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
            Nonce = "0x1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef"
        };

        // 64 bytes instead of 65
        var shortSignature = "0x" + new string('a', 128);

        // Act
        var result = _verifier.Verify(authorization, shortSignature, TestNetwork, TestUsdcContract);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Verify_DifferentNetwork_RecoversDifferentAddress()
    {
        // Arrange - sign for Base Sepolia
        var authorization = new X402Eip3009Authorization
        {
            From = TestAddress,
            To = "0x70997970C51812dc3A010C7d01b50e0d17dc79C8",
            Value = "1000000",
            ValidAfter = 0,
            ValidBefore = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
            Nonce = "0x" + Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N").Substring(0, 32)
        };

        var signature = SignEip3009Authorization(authorization, TestPrivateKey, X402Networks.BaseSepolia, TestUsdcContract);

        // Act - verify on different network (different chain ID = different domain separator)
        var result = _verifier.Verify(authorization, signature, X402Networks.EthereumMainnet, X402Assets.UsdcContracts[X402Networks.EthereumMainnet]);

        // Assert - should fail because domain separator is different
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("1000000")]      // 1 USDC
    [InlineData("100")]          // 0.0001 USDC
    [InlineData("1000000000")]   // 1000 USDC
    public void Verify_VariousAmounts_ValidSignatures(string amount)
    {
        // Arrange
        var authorization = new X402Eip3009Authorization
        {
            From = TestAddress,
            To = "0x70997970C51812dc3A010C7d01b50e0d17dc79C8",
            Value = amount,
            ValidAfter = 0,
            ValidBefore = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
            Nonce = "0x" + Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N").Substring(0, 32)
        };

        var signature = SignEip3009Authorization(authorization, TestPrivateKey, TestNetwork, TestUsdcContract);

        // Act
        var result = _verifier.Verify(authorization, signature, TestNetwork, TestUsdcContract);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #region Helper Methods

    /// <summary>
    /// Signs an EIP-3009 authorization using EIP-712 typed data signing.
    /// This mimics what a wallet would do when signing a transferWithAuthorization.
    /// </summary>
    private static string SignEip3009Authorization(
        X402Eip3009Authorization auth,
        string privateKey,
        string network,
        string tokenContract)
    {
        var sha3 = new Sha3Keccack();

        // Get chain ID for network
        var chainId = X402Networks.ChainIds.TryGetValue(network, out var id) ? id : 1;

        // Build domain separator
        var domainSeparator = BuildDomainSeparator(sha3, "USD Coin", "2", chainId, tokenContract);

        // Build struct hash
        var structHash = BuildStructHash(sha3, auth);

        // Build EIP-712 digest
        var digest = BuildEip712Digest(sha3, domainSeparator, structHash);

        // Sign with private key
        var key = new EthECKey(privateKey);
        var signature = key.SignAndCalculateV(digest);

        // Combine r + s + v into 65-byte signature
        // signature.R and signature.S are already byte arrays
        var r = signature.R;
        var s = signature.S;
        var v = signature.V[0];

        // Pad r and s to 32 bytes (they should already be 32, but just in case)
        var sigBytes = new byte[65];
        Array.Copy(r, 0, sigBytes, 32 - r.Length, r.Length);
        Array.Copy(s, 0, sigBytes, 64 - s.Length, s.Length);
        sigBytes[64] = v;

        return "0x" + BitConverter.ToString(sigBytes).Replace("-", "").ToLower();
    }

    private static byte[] BuildDomainSeparator(Sha3Keccack sha3, string name, string version, int chainId, string verifyingContract)
    {
        var domainTypeHash = sha3.CalculateHash(
            System.Text.Encoding.UTF8.GetBytes("EIP712Domain(string name,string version,uint256 chainId,address verifyingContract)"));

        var nameHash = sha3.CalculateHash(System.Text.Encoding.UTF8.GetBytes(name));
        var versionHash = sha3.CalculateHash(System.Text.Encoding.UTF8.GetBytes(version));

        var encoded = new byte[32 * 5];
        Array.Copy(domainTypeHash, 0, encoded, 0, 32);
        Array.Copy(nameHash, 0, encoded, 32, 32);
        Array.Copy(versionHash, 0, encoded, 64, 32);

        var chainIdBytes = new BigInteger(chainId).ToByteArray(isUnsigned: true, isBigEndian: true);
        Array.Copy(chainIdBytes, 0, encoded, 96 + (32 - chainIdBytes.Length), chainIdBytes.Length);

        var contractBytes = HexToBytes(verifyingContract);
        Array.Copy(contractBytes, 0, encoded, 128 + (32 - contractBytes.Length), contractBytes.Length);

        return sha3.CalculateHash(encoded);
    }

    private static byte[] BuildStructHash(Sha3Keccack sha3, X402Eip3009Authorization auth)
    {
        var typeHash = sha3.CalculateHash(
            System.Text.Encoding.UTF8.GetBytes("TransferWithAuthorization(address from,address to,uint256 value,uint256 validAfter,uint256 validBefore,bytes32 nonce)"));

        var encoded = new byte[32 * 7];
        Array.Copy(typeHash, 0, encoded, 0, 32);

        var fromBytes = HexToBytes(auth.From);
        Array.Copy(fromBytes, 0, encoded, 32 + (32 - fromBytes.Length), fromBytes.Length);

        var toBytes = HexToBytes(auth.To);
        Array.Copy(toBytes, 0, encoded, 64 + (32 - toBytes.Length), toBytes.Length);

        var valueBytes = BigInteger.Parse(auth.Value).ToByteArray(isUnsigned: true, isBigEndian: true);
        Array.Copy(valueBytes, 0, encoded, 96 + (32 - valueBytes.Length), valueBytes.Length);

        var validAfterBytes = new BigInteger(auth.ValidAfter).ToByteArray(isUnsigned: true, isBigEndian: true);
        Array.Copy(validAfterBytes, 0, encoded, 128 + (32 - validAfterBytes.Length), validAfterBytes.Length);

        var validBeforeBytes = new BigInteger(auth.ValidBefore).ToByteArray(isUnsigned: true, isBigEndian: true);
        Array.Copy(validBeforeBytes, 0, encoded, 160 + (32 - validBeforeBytes.Length), validBeforeBytes.Length);

        var nonceBytes = HexToBytes(auth.Nonce);
        if (nonceBytes.Length < 32)
        {
            var paddedNonce = new byte[32];
            Array.Copy(nonceBytes, 0, paddedNonce, 32 - nonceBytes.Length, nonceBytes.Length);
            nonceBytes = paddedNonce;
        }
        Array.Copy(nonceBytes, 0, encoded, 192, 32);

        return sha3.CalculateHash(encoded);
    }

    private static byte[] BuildEip712Digest(Sha3Keccack sha3, byte[] domainSeparator, byte[] structHash)
    {
        var message = new byte[66];
        message[0] = 0x19;
        message[1] = 0x01;
        Array.Copy(domainSeparator, 0, message, 2, 32);
        Array.Copy(structHash, 0, message, 34, 32);
        return sha3.CalculateHash(message);
    }

    private static byte[] HexToBytes(string hex)
    {
        if (string.IsNullOrEmpty(hex)) return Array.Empty<byte>();
        if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) hex = hex[2..];
        if (hex.Length % 2 != 0) hex = "0" + hex;

        var bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }
        return bytes;
    }

    #endregion
}
