using AgenticCommerce.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace AgenticCommerce.Core.Interfaces
{
    public interface IX402FacilitatorClient
    {
        /// <summary>
        /// Verify an x402 payment request
        /// </summary>
        /// <param name="paymentRequest"></param>
        /// <returns></returns>
        Task<X402VerificationResult> VerifyPaymentRequestAsync(string paymentRequest);

        /// <summary>
        /// Submits the specified payment for processing asynchronously using the X402 payment protocol.        
        /// </summary>
        /// <param name="payment">The payment details to be processed. Cannot be null.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an X402PaymentResult object with
        /// the outcome of the payment submission.</returns>
        Task<X402PaymentResult> SubmitPaymentAsync(X402Payment payment);
    }
}
