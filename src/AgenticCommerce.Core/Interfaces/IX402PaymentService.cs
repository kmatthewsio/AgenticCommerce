using AgenticCommerce.Core.Models;

namespace AgenticCommerce.Core.Interfaces
{
    public interface IX402PaymentService
    {
        /// <summary>
        /// Create a new payment requirement
        /// </summary>
        PaymentRequirement CreatePaymentRequirement(string endpoint, decimal amount, string description);

        /// <summary>
        /// Verify a payment proof
        /// </summary>
        Task<PaymentVerificationResult> VerifyPaymentAsync(PaymentProof proof);

        /// <summary>
        /// Mark a payment as completed
        /// </summary>
        void CompletePayment(string paymentId, string transactionId);
    }
}
