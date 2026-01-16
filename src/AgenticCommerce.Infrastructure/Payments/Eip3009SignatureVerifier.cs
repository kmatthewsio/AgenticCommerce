using System.Numerics;
using AgenticCommerce.Core.Models;
using Microsoft.Extensions.Logging;
using Nethereum.Signer;
using Nethereum.Util;

namespace AgenticCommerce.Infrastructure.Payments;

/// <summary>
/// Verifies EIP-3009 (transferWithAuthorization) signatures off-chain
/// using EIP-712 typed structured data hashing
/// </summary>
public interface IEip3009SignatureVerifier
{
    /// <summary>
    /// Verify that the signature in the payload was signed by the claimed 'from' address
    /// </summary>
    Eip3009VerificationResult Verify(
        X402Eip3009Authorization authorization,
        string signature,
        string network,
        string tokenContract);
}

public class Eip3009VerificationResult
{
    public bool IsValid { get; set; }
    public string? RecoveredAddress { get; set; }
    public string? ErrorMessage { get; set; }
}

public class Eip3009SignatureVerifier : IEip3009SignatureVerifier
{
    private readonly ILogger<Eip3009SignatureVerifier> _logger;

    // EIP-712 type hashes (pre-computed keccak256 of type strings)
    // TransferWithAuthorization(address from,address to,uint256 value,uint256 validAfter,uint256 validBefore,bytes32 nonce)
    private static readonly byte[] TransferWithAuthorizationTypeHash = new Sha3Keccack().CalculateHash(
        System.Text.Encoding.UTF8.GetBytes(
            "TransferWithAuthorization(address from,address to,uint256 value,uint256 validAfter,uint256 validBefore,bytes32 nonce)"));

    // EIP712Domain(string name,string version,uint256 chainId,address verifyingContract)
    private static readonly byte[] Eip712DomainTypeHash = new Sha3Keccack().CalculateHash(
        System.Text.Encoding.UTF8.GetBytes(
            "EIP712Domain(string name,string version,uint256 chainId,address verifyingContract)"));

    // USDC token metadata by network (name, version, chainId for EIP-712 domain)
    private static readonly Dictionary<string, (string Name, string Version, int ChainId)> TokenMetadata = new()
    {
        [X402Networks.EthereumMainnet] = ("USD Coin", "2", 1),
        [X402Networks.EthereumSepolia] = ("USD Coin", "2", 11155111),
        [X402Networks.BaseMainnet] = ("USD Coin", "2", 8453),
        [X402Networks.BaseSepolia] = ("USD Coin", "2", 84532),
        [X402Networks.ArcTestnet] = ("USD Coin", "2", 5042002),  // Arc testnet official chain ID
        [X402Networks.ArcMainnet] = ("USD Coin", "2", 0),        // Arc mainnet not yet launched
    };

    public Eip3009SignatureVerifier(ILogger<Eip3009SignatureVerifier> logger)
    {
        _logger = logger;
    }

    public Eip3009VerificationResult Verify(
        X402Eip3009Authorization authorization,
        string signature,
        string network,
        string tokenContract)
    {
        try
        {
            _logger.LogDebug(
                "Verifying EIP-3009 signature for transfer from {From} to {To}, value {Value}",
                authorization.From, authorization.To, authorization.Value);

            // Validate signature format
            if (string.IsNullOrEmpty(signature) || !signature.StartsWith("0x"))
            {
                return new Eip3009VerificationResult
                {
                    IsValid = false,
                    ErrorMessage = "Invalid signature format - must be hex string starting with 0x"
                };
            }

            // Get network metadata
            if (!TokenMetadata.TryGetValue(network, out var metadata))
            {
                _logger.LogWarning("Unknown network {Network}, using default metadata", network);
                metadata = ("USD Coin", "2", 1); // Default fallback
            }

            // Build EIP-712 domain separator
            var domainSeparator = BuildDomainSeparator(
                metadata.Name,
                metadata.Version,
                metadata.ChainId,
                tokenContract);

            // Build struct hash for the authorization
            var structHash = BuildTransferWithAuthorizationStructHash(authorization);

            // Build final EIP-712 digest: keccak256("\x19\x01" + domainSeparator + structHash)
            var digest = BuildEip712Digest(domainSeparator, structHash);

            // Recover signer address from signature
            var recoveredAddress = RecoverSignerAddress(digest, signature);

            if (string.IsNullOrEmpty(recoveredAddress))
            {
                return new Eip3009VerificationResult
                {
                    IsValid = false,
                    ErrorMessage = "Failed to recover signer address from signature"
                };
            }

            // Compare recovered address with claimed 'from' address (case-insensitive)
            var isValid = string.Equals(
                recoveredAddress,
                authorization.From,
                StringComparison.OrdinalIgnoreCase);

            if (!isValid)
            {
                _logger.LogWarning(
                    "Signature verification failed: recovered {Recovered}, expected {Expected}",
                    recoveredAddress, authorization.From);
            }
            else
            {
                _logger.LogInformation(
                    "Signature verified successfully for {Address}",
                    recoveredAddress);
            }

            return new Eip3009VerificationResult
            {
                IsValid = isValid,
                RecoveredAddress = recoveredAddress,
                ErrorMessage = isValid ? null : $"Signature does not match claimed sender. Recovered: {recoveredAddress}, Expected: {authorization.From}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying EIP-3009 signature");
            return new Eip3009VerificationResult
            {
                IsValid = false,
                ErrorMessage = $"Signature verification error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Build EIP-712 domain separator hash
    /// </summary>
    private byte[] BuildDomainSeparator(string name, string version, int chainId, string verifyingContract)
    {
        var sha3 = new Sha3Keccack();

        // Hash the name and version strings
        var nameHash = sha3.CalculateHash(System.Text.Encoding.UTF8.GetBytes(name));
        var versionHash = sha3.CalculateHash(System.Text.Encoding.UTF8.GetBytes(version));

        // Encode: keccak256(typeHash + nameHash + versionHash + chainId + verifyingContract)
        var encoded = new byte[32 * 5]; // 5 x 32-byte values

        Array.Copy(Eip712DomainTypeHash, 0, encoded, 0, 32);
        Array.Copy(nameHash, 0, encoded, 32, 32);
        Array.Copy(versionHash, 0, encoded, 64, 32);

        // chainId as uint256 (big-endian, 32 bytes)
        var chainIdBytes = new BigInteger(chainId).ToByteArray(isUnsigned: true, isBigEndian: true);
        Array.Copy(chainIdBytes, 0, encoded, 96 + (32 - chainIdBytes.Length), chainIdBytes.Length);

        // verifyingContract as address (20 bytes, right-padded in 32 bytes)
        var contractBytes = HexToBytes(verifyingContract);
        Array.Copy(contractBytes, 0, encoded, 128 + (32 - contractBytes.Length), contractBytes.Length);

        return sha3.CalculateHash(encoded);
    }

    /// <summary>
    /// Build struct hash for TransferWithAuthorization
    /// </summary>
    private byte[] BuildTransferWithAuthorizationStructHash(X402Eip3009Authorization auth)
    {
        var sha3 = new Sha3Keccack();

        // Encode: keccak256(typeHash + from + to + value + validAfter + validBefore + nonce)
        var encoded = new byte[32 * 7]; // 7 x 32-byte values

        Array.Copy(TransferWithAuthorizationTypeHash, 0, encoded, 0, 32);

        // from address (20 bytes, left-padded to 32)
        var fromBytes = HexToBytes(auth.From);
        Array.Copy(fromBytes, 0, encoded, 32 + (32 - fromBytes.Length), fromBytes.Length);

        // to address (20 bytes, left-padded to 32)
        var toBytes = HexToBytes(auth.To);
        Array.Copy(toBytes, 0, encoded, 64 + (32 - toBytes.Length), toBytes.Length);

        // value as uint256
        var valueBytes = BigInteger.Parse(auth.Value).ToByteArray(isUnsigned: true, isBigEndian: true);
        Array.Copy(valueBytes, 0, encoded, 96 + (32 - valueBytes.Length), valueBytes.Length);

        // validAfter as uint256
        var validAfterBytes = new BigInteger(auth.ValidAfter).ToByteArray(isUnsigned: true, isBigEndian: true);
        Array.Copy(validAfterBytes, 0, encoded, 128 + (32 - validAfterBytes.Length), validAfterBytes.Length);

        // validBefore as uint256
        var validBeforeBytes = new BigInteger(auth.ValidBefore).ToByteArray(isUnsigned: true, isBigEndian: true);
        Array.Copy(validBeforeBytes, 0, encoded, 160 + (32 - validBeforeBytes.Length), validBeforeBytes.Length);

        // nonce as bytes32 (the nonce in EIP-3009 is bytes32)
        var nonceBytes = HexToBytes(auth.Nonce);
        if (nonceBytes.Length < 32)
        {
            // Left-pad to 32 bytes if needed
            var paddedNonce = new byte[32];
            Array.Copy(nonceBytes, 0, paddedNonce, 32 - nonceBytes.Length, nonceBytes.Length);
            nonceBytes = paddedNonce;
        }
        Array.Copy(nonceBytes, 0, encoded, 192, 32);

        return sha3.CalculateHash(encoded);
    }

    /// <summary>
    /// Build final EIP-712 digest: keccak256("\x19\x01" + domainSeparator + structHash)
    /// </summary>
    private byte[] BuildEip712Digest(byte[] domainSeparator, byte[] structHash)
    {
        var sha3 = new Sha3Keccack();

        // "\x19\x01" prefix + domainSeparator (32) + structHash (32) = 66 bytes
        var message = new byte[66];
        message[0] = 0x19;
        message[1] = 0x01;
        Array.Copy(domainSeparator, 0, message, 2, 32);
        Array.Copy(structHash, 0, message, 34, 32);

        return sha3.CalculateHash(message);
    }

    /// <summary>
    /// Recover the signer's address from an ECDSA signature
    /// </summary>
    private string? RecoverSignerAddress(byte[] digest, string signature)
    {
        try
        {
            var sigBytes = HexToBytes(signature);

            // Signature should be 65 bytes: r (32) + s (32) + v (1)
            if (sigBytes.Length != 65)
            {
                _logger.LogWarning("Invalid signature length: {Length}, expected 65", sigBytes.Length);
                return null;
            }

            var r = new byte[32];
            var s = new byte[32];
            Array.Copy(sigBytes, 0, r, 0, 32);
            Array.Copy(sigBytes, 32, s, 0, 32);
            var v = sigBytes[64];

            // Normalize v value (could be 0/1 or 27/28)
            if (v < 27)
            {
                v += 27;
            }

            var ecKey = EthECKey.RecoverFromSignature(
                EthECDSASignatureFactory.FromComponents(r, s, v),
                digest);

            if (ecKey == null)
            {
                _logger.LogWarning("Failed to recover public key from signature");
                return null;
            }

            return ecKey.GetPublicAddress();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recovering signer address");
            return null;
        }
    }

    /// <summary>
    /// Convert hex string to bytes
    /// </summary>
    private static byte[] HexToBytes(string hex)
    {
        if (string.IsNullOrEmpty(hex))
            return Array.Empty<byte>();

        // Remove 0x prefix if present
        if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            hex = hex[2..];

        // Handle odd-length hex strings
        if (hex.Length % 2 != 0)
            hex = "0" + hex;

        var bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }
        return bytes;
    }
}
