using AgenticCommerce.Core.Interfaces;
using AgenticCommerce.Core.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace AgenticCommerce.Infrastructure.Payments;

public class X402PaymentService : IX402PaymentService
{
    private readonly IArcClient _arcClient;
    private readonly ILogger<X402PaymentService> _logger;

    // In-memory storage for demo (would be database in production)
    private readonly ConcurrentDictionary<string, PaymentRequirement> _pendingPayments = new();
    private readonly ConcurrentDictionary<string, PaymentProof> _completedPayments = new();

    public X402PaymentService(IArcClient arcClient, ILogger<X402PaymentService> logger)
    {
        _arcClient = arcClient;
        _logger = logger;
    }

    public PaymentRequirement CreatePaymentRequirement(string endpoint, decimal amount, string description)
    {
        var requirement = new PaymentRequirement
        {
            PaymentId = $"pay_{Guid.NewGuid():N}",
            Amount = amount,
            RecipientAddress = _arcClient.GetAddress(), // Payment goes to our wallet
            Description = description,
            Endpoint = endpoint,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5), // 5 minute expiry
            Metadata = new Dictionary<string, string>
            {
                { "created_at", DateTime.UtcNow.ToString("O") },
                { "protocol", "x402" }
            }
        };

        _pendingPayments.TryAdd(requirement.PaymentId, requirement);

        _logger.LogInformation(
            "Created payment requirement {PaymentId} for {Amount} USDC on {Endpoint}",
            requirement.PaymentId, requirement.Amount, requirement.Endpoint);

        return requirement;
    }

    public async Task<PaymentVerificationResult> VerifyPaymentAsync(PaymentProof proof)
    {
        _logger.LogInformation("Verifying payment proof for {PaymentId}", proof.PaymentId);

        // Check if already completed
        if (_completedPayments.TryGetValue(proof.PaymentId, out var existingProof))
        {
            _logger.LogInformation("Payment {PaymentId} already verified", proof.PaymentId);

            return new PaymentVerificationResult
            {
                IsValid = true,
                Proof = existingProof,
                VerifiedAt = existingProof.PaidAt
            };
        }

        // Check if payment requirement exists
        if (!_pendingPayments.TryGetValue(proof.PaymentId, out var requirement))
        {
            _logger.LogWarning(
                "Payment requirement not found: {PaymentId}. Pending: {PendingCount}, Completed: {CompletedCount}",
                proof.PaymentId,
                _pendingPayments.Count,
                _completedPayments.Count);

            return new PaymentVerificationResult
            {
                IsValid = false,
                ErrorMessage = $"Payment requirement not found. It may have expired or never existed. (Pending: {_pendingPayments.Count}, Completed: {_completedPayments.Count})"
            };
        }

        // Check if expired
        if (requirement.ExpiresAt < DateTime.UtcNow)
        {
            _pendingPayments.TryRemove(proof.PaymentId, out _);
            return new PaymentVerificationResult
            {
                IsValid = false,
                ErrorMessage = "Payment requirement expired"
            };
        }

        // Verify amount matches
        if (proof.Amount < requirement.Amount)
        {
            return new PaymentVerificationResult
            {
                IsValid = false,
                ErrorMessage = $"Insufficient amount. Required: {requirement.Amount}, Paid: {proof.Amount}"
            };
        }

        // Verify recipient matches
        if (proof.RecipientAddress.ToLower() != requirement.RecipientAddress.ToLower())
        {
            return new PaymentVerificationResult
            {
                IsValid = false,
                ErrorMessage = "Recipient address mismatch"
            };
        }

        // In production, you'd verify the transaction on Arc blockchain
        if (string.IsNullOrEmpty(proof.TransactionId))
        {
            return new PaymentVerificationResult
            {
                IsValid = false,
                ErrorMessage = "Missing transaction ID"
            };
        }

        // Mark as completed
        _completedPayments.TryAdd(proof.PaymentId, proof);
        _pendingPayments.TryRemove(proof.PaymentId, out _);

        _logger.LogInformation(
            "Payment verified: {PaymentId} - TX: {TransactionId}",
            proof.PaymentId, proof.TransactionId);

        return new PaymentVerificationResult
        {
            IsValid = true,
            Proof = proof,
            VerifiedAt = DateTime.UtcNow
        };
    }

    public void CompletePayment(string paymentId, string transactionId)
    {
        if (_pendingPayments.TryGetValue(paymentId, out var requirement))
        {
            var proof = new PaymentProof
            {
                PaymentId = paymentId,
                TransactionId = transactionId,
                Amount = requirement.Amount,
                RecipientAddress = requirement.RecipientAddress,
                SenderAddress = _arcClient.GetAddress(),
                PaidAt = DateTime.UtcNow
            };

            _completedPayments.TryAdd(paymentId, proof);
            _pendingPayments.TryRemove(paymentId, out _);
        }
    }
}