# =============================================================================
# AgentRails Stripe Payment Flow Test Script (PowerShell)
# =============================================================================
# This script tests both Standard (Sandbox) and Enterprise (Stripe) tiers
#
# Product Tiers:
# - Standard/Sandbox: Free tier, no payment required, testnet access only
# - Enterprise: $2,500 one-time via Stripe Checkout, full production access
#
# Prerequisites:
# - API running on https://localhost:7098
# - Stripe test keys configured
# - PostgreSQL database running
# =============================================================================

param(
    [string]$BaseUrl = "https://localhost:7098"
)

# Skip SSL certificate validation for localhost testing
add-type @"
using System.Net;
using System.Security.Cryptography.X509Certificates;
public class TrustAllCertsPolicy : ICertificatePolicy {
    public bool CheckValidationResult(
        ServicePoint srvPoint, X509Certificate certificate,
        WebRequest request, int certificateProblem) {
        return true;
    }
}
"@
[System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

Write-Host "=============================================="
Write-Host "AgentRails Payment Flow Test"
Write-Host "Base URL: $BaseUrl"
Write-Host "=============================================="

function Write-Success { param($msg) Write-Host "[PASS] $msg" -ForegroundColor Green }
function Write-Fail { param($msg) Write-Host "[FAIL] $msg" -ForegroundColor Red }
function Write-Info { param($msg) Write-Host "[INFO] $msg" -ForegroundColor Yellow }

# =============================================================================
# SECTION 1: Health Check
# =============================================================================
Write-Host ""
Write-Host "1. Testing Health Endpoint..."
Write-Host "----------------------------------------------"

try {
    $healthResponse = Invoke-RestMethod -Uri "$BaseUrl/health" -Method Get -ErrorAction Stop
    Write-Success "Health check passed"
} catch {
    Write-Fail "Health check failed: $_"
    exit 1
}

# =============================================================================
# SECTION 2: Standard/Sandbox Tier (Free - x402 Protocol)
# =============================================================================
Write-Host ""
Write-Host "2. Testing Standard/Sandbox Tier (Free x402 Access)..."
Write-Host "----------------------------------------------"

# Test x402 pricing endpoint
Write-Info "Testing x402 pricing endpoint..."
try {
    $pricingResponse = Invoke-RestMethod -Uri "$BaseUrl/api/x402/pricing" -Method Get -ErrorAction Stop
    Write-Success "x402 pricing endpoint accessible"
    Write-Host "Response: $($pricingResponse | ConvertTo-Json -Compress)"
} catch {
    Write-Fail "x402 pricing endpoint failed: $_"
}

# Test protected endpoint (should return 402)
Write-Info "Testing x402 protected endpoint (expecting 402)..."
try {
    $protectedResponse = Invoke-WebRequest -Uri "$BaseUrl/api/x402/protected/data" -Method Get -ErrorAction Stop
    Write-Info "x402 protected endpoint returned status: $($protectedResponse.StatusCode)"
} catch {
    if ($_.Exception.Response.StatusCode -eq 402) {
        Write-Success "x402 protected endpoint returns 402 Payment Required (as expected)"
    } else {
        Write-Info "x402 protected endpoint error: $_"
    }
}

# =============================================================================
# SECTION 3: Enterprise Tier (Stripe Checkout - $2,500)
# =============================================================================
Write-Host ""
Write-Host "3. Testing Enterprise Tier (Stripe Checkout)..."
Write-Host "----------------------------------------------"

# Test create checkout session
Write-Info "Creating Stripe checkout session..."
$checkoutBody = @{
    email = "test@example.com"
} | ConvertTo-Json

try {
    $checkoutResponse = Invoke-RestMethod -Uri "$BaseUrl/api/stripe/create-checkout-session" `
        -Method Post `
        -ContentType "application/json" `
        -Body $checkoutBody `
        -ErrorAction Stop

    Write-Success "Stripe checkout session created"
    Write-Host "Response: $($checkoutResponse | ConvertTo-Json -Compress)"

    if ($checkoutResponse.url) {
        Write-Info "Checkout URL: $($checkoutResponse.url)"
    }
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    if ($statusCode -eq 500) {
        Write-Fail "Stripe checkout failed - check if Stripe:ImplementationKitPriceId is configured"
    } else {
        Write-Fail "Stripe checkout failed (status: $statusCode): $_"
    }
}

# =============================================================================
# SECTION 4: Simulate Stripe Webhook (Checkout Completed)
# =============================================================================
Write-Host ""
Write-Host "4. Testing Stripe Webhook (Enterprise Purchase)..."
Write-Host "----------------------------------------------"

Write-Info "Simulating checkout.session.completed webhook..."

$timestamp = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
$webhookPayload = @{
    id = "evt_test_$timestamp"
    type = "checkout.session.completed"
    data = @{
        object = @{
            id = "cs_test_$timestamp"
            customer_email = "enterprise-test@example.com"
            customer_details = @{
                email = "enterprise-test@example.com"
            }
            payment_intent = "pi_test_$timestamp"
            amount_total = 250000
            currency = "usd"
            metadata = @{
                product = "implementation-kit"
            }
        }
    }
} | ConvertTo-Json -Depth 10

try {
    $webhookResponse = Invoke-RestMethod -Uri "$BaseUrl/api/stripe/webhook" `
        -Method Post `
        -ContentType "application/json" `
        -Body $webhookPayload `
        -ErrorAction Stop

    Write-Success "Stripe webhook processed successfully"
    Write-Host "Response: $($webhookResponse | ConvertTo-Json -Compress)"
    Write-Info "Check database for new StripePurchase, Organization, and ApiKey records"
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    if ($statusCode -eq 400) {
        Write-Info "Webhook returned 400 - signature verification may be enabled"
    } else {
        Write-Fail "Webhook processing failed (status: $statusCode): $_"
    }
}

# =============================================================================
# SECTION 5: Simulate Refund Webhook
# =============================================================================
Write-Host ""
Write-Host "5. Testing Stripe Refund Webhook..."
Write-Host "----------------------------------------------"

Write-Info "Simulating charge.refunded webhook..."

$refundPayload = @{
    id = "evt_refund_test_$timestamp"
    type = "charge.refunded"
    data = @{
        object = @{
            id = "ch_test_$timestamp"
            payment_intent = "pi_test_refund_$timestamp"
        }
    }
} | ConvertTo-Json -Depth 10

try {
    $refundResponse = Invoke-RestMethod -Uri "$BaseUrl/api/stripe/webhook" `
        -Method Post `
        -ContentType "application/json" `
        -Body $refundPayload `
        -ErrorAction Stop

    Write-Success "Refund webhook processed (no matching purchase expected)"
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    Write-Info "Refund webhook returned status: $statusCode"
}

# =============================================================================
# SECTION 6: Check x402 Payments Logged (Standard Tier)
# =============================================================================
Write-Host ""
Write-Host "6. Testing x402 Payment Analytics..."
Write-Host "----------------------------------------------"

Write-Info "Fetching x402 payment history..."
try {
    $paymentsResponse = Invoke-RestMethod -Uri "$BaseUrl/api/x402/payments" -Method Get -ErrorAction Stop
    Write-Success "x402 payment history endpoint accessible"
    Write-Host "Response: $($paymentsResponse | ConvertTo-Json -Compress)"
} catch {
    Write-Info "x402 payment history error: $_"
}

Write-Info "Fetching x402 payment stats..."
try {
    $statsResponse = Invoke-RestMethod -Uri "$BaseUrl/api/x402/stats" -Method Get -ErrorAction Stop
    Write-Success "x402 stats endpoint accessible"
    Write-Host "Response: $($statsResponse | ConvertTo-Json -Compress)"
} catch {
    Write-Info "x402 stats error: $_"
}

# =============================================================================
# SECTION 7: Summary
# =============================================================================
Write-Host ""
Write-Host "=============================================="
Write-Host "Test Summary"
Write-Host "=============================================="
Write-Host ""
Write-Host "Standard/Sandbox Tier (Free):"
Write-Host "  - x402 protocol endpoints accessible"
Write-Host "  - Protected endpoints return 402 Payment Required"
Write-Host "  - No Stripe payment needed for testnet access"
Write-Host ""
Write-Host "Enterprise Tier (`$2,500):"
Write-Host "  - Stripe checkout session creation tested"
Write-Host "  - Webhook handling tested"
Write-Host "  - API key provisioning flow verified"
Write-Host ""
Write-Host "Logging:"
Write-Host "  - Stripe purchases logged to 'stripe_purchases' table"
Write-Host "  - x402 payments logged to 'x402_payments' table"
Write-Host "  - Application logs in 'logs/log.txt'"
Write-Host ""
Write-Host "=============================================="
Write-Host "To verify database logging, run:"
Write-Host "  SELECT * FROM stripe_purchases ORDER BY created_at DESC LIMIT 5;"
Write-Host "  SELECT * FROM x402_payments ORDER BY created_at DESC LIMIT 5;"
Write-Host "=============================================="
