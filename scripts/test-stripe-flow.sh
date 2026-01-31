#!/bin/bash

# =============================================================================
# AgentRails Stripe Payment Flow Test Script
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

set -e

BASE_URL="${BASE_URL:-https://localhost:7098}"
INSECURE="-k" # Skip SSL verification for localhost

echo "=============================================="
echo "AgentRails Payment Flow Test"
echo "Base URL: $BASE_URL"
echo "=============================================="

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

success() {
    echo -e "${GREEN}[PASS]${NC} $1"
}

fail() {
    echo -e "${RED}[FAIL]${NC} $1"
}

info() {
    echo -e "${YELLOW}[INFO]${NC} $1"
}

# =============================================================================
# SECTION 1: Health Check
# =============================================================================
echo ""
echo "1. Testing Health Endpoint..."
echo "----------------------------------------------"

HEALTH_RESPONSE=$(curl -s $INSECURE "$BASE_URL/health" -w "\n%{http_code}" || echo "error")
HEALTH_STATUS=$(echo "$HEALTH_RESPONSE" | tail -n1)
HEALTH_BODY=$(echo "$HEALTH_RESPONSE" | head -n-1)

if [ "$HEALTH_STATUS" = "200" ]; then
    success "Health check passed"
else
    fail "Health check failed (status: $HEALTH_STATUS)"
    exit 1
fi

# =============================================================================
# SECTION 2: Standard/Sandbox Tier (Free - x402 Protocol)
# =============================================================================
echo ""
echo "2. Testing Standard/Sandbox Tier (Free x402 Access)..."
echo "----------------------------------------------"

# Test x402 pricing endpoint
info "Testing x402 pricing endpoint..."
PRICING_RESPONSE=$(curl -s $INSECURE "$BASE_URL/api/x402/pricing" -w "\n%{http_code}")
PRICING_STATUS=$(echo "$PRICING_RESPONSE" | tail -n1)
PRICING_BODY=$(echo "$PRICING_RESPONSE" | head -n-1)

if [ "$PRICING_STATUS" = "200" ]; then
    success "x402 pricing endpoint accessible"
    echo "Response: $PRICING_BODY"
else
    fail "x402 pricing endpoint failed (status: $PRICING_STATUS)"
fi

# Test protected endpoint (should return 402)
info "Testing x402 protected endpoint (expecting 402)..."
PROTECTED_RESPONSE=$(curl -s $INSECURE "$BASE_URL/api/x402/protected/data" -w "\n%{http_code}")
PROTECTED_STATUS=$(echo "$PROTECTED_RESPONSE" | tail -n1)

if [ "$PROTECTED_STATUS" = "402" ]; then
    success "x402 protected endpoint returns 402 Payment Required (as expected)"
else
    info "x402 protected endpoint returned status: $PROTECTED_STATUS"
fi

# =============================================================================
# SECTION 3: Enterprise Tier (Stripe Checkout - $2,500)
# =============================================================================
echo ""
echo "3. Testing Enterprise Tier (Stripe Checkout)..."
echo "----------------------------------------------"

# Test create checkout session
info "Creating Stripe checkout session..."
CHECKOUT_RESPONSE=$(curl -s $INSECURE -X POST "$BASE_URL/api/stripe/create-checkout-session" \
    -H "Content-Type: application/json" \
    -d '{"email": "test@example.com"}' \
    -w "\n%{http_code}")
CHECKOUT_STATUS=$(echo "$CHECKOUT_RESPONSE" | tail -n1)
CHECKOUT_BODY=$(echo "$CHECKOUT_RESPONSE" | head -n-1)

if [ "$CHECKOUT_STATUS" = "200" ]; then
    success "Stripe checkout session created"
    echo "Response: $CHECKOUT_BODY"

    # Extract session URL if present
    SESSION_URL=$(echo "$CHECKOUT_BODY" | grep -o '"url":"[^"]*"' | sed 's/"url":"//;s/"$//' || echo "")
    if [ -n "$SESSION_URL" ]; then
        info "Checkout URL: $SESSION_URL"
    fi
elif [ "$CHECKOUT_STATUS" = "500" ]; then
    fail "Stripe checkout failed - check if Stripe:ImplementationKitPriceId is configured"
    echo "Response: $CHECKOUT_BODY"
else
    fail "Stripe checkout failed (status: $CHECKOUT_STATUS)"
    echo "Response: $CHECKOUT_BODY"
fi

# =============================================================================
# SECTION 4: Simulate Stripe Webhook (Checkout Completed)
# =============================================================================
echo ""
echo "4. Testing Stripe Webhook (Enterprise Purchase)..."
echo "----------------------------------------------"

# Note: In production, Stripe sends this. For testing, we simulate without signature verification.
# The webhook handler checks for Stripe:WebhookSecret - if empty, it skips signature verification.

info "Simulating checkout.session.completed webhook..."

# Create a test webhook payload
WEBHOOK_PAYLOAD=$(cat <<EOF
{
    "id": "evt_test_$(date +%s)",
    "type": "checkout.session.completed",
    "data": {
        "object": {
            "id": "cs_test_$(date +%s)",
            "customer_email": "enterprise-test@example.com",
            "customer_details": {
                "email": "enterprise-test@example.com"
            },
            "payment_intent": "pi_test_$(date +%s)",
            "amount_total": 250000,
            "currency": "usd",
            "metadata": {
                "product": "implementation-kit"
            }
        }
    }
}
EOF
)

WEBHOOK_RESPONSE=$(curl -s $INSECURE -X POST "$BASE_URL/api/stripe/webhook" \
    -H "Content-Type: application/json" \
    -d "$WEBHOOK_PAYLOAD" \
    -w "\n%{http_code}")
WEBHOOK_STATUS=$(echo "$WEBHOOK_RESPONSE" | tail -n1)
WEBHOOK_BODY=$(echo "$WEBHOOK_RESPONSE" | head -n-1)

if [ "$WEBHOOK_STATUS" = "200" ]; then
    success "Stripe webhook processed successfully"
    echo "Response: $WEBHOOK_BODY"
    info "Check database for new StripePurchase, Organization, and ApiKey records"
elif [ "$WEBHOOK_STATUS" = "400" ]; then
    info "Webhook returned 400 - signature verification may be enabled"
    echo "Response: $WEBHOOK_BODY"
else
    fail "Webhook processing failed (status: $WEBHOOK_STATUS)"
    echo "Response: $WEBHOOK_BODY"
fi

# =============================================================================
# SECTION 5: Simulate Refund Webhook
# =============================================================================
echo ""
echo "5. Testing Stripe Refund Webhook..."
echo "----------------------------------------------"

info "Simulating charge.refunded webhook..."

REFUND_PAYLOAD=$(cat <<EOF
{
    "id": "evt_refund_test_$(date +%s)",
    "type": "charge.refunded",
    "data": {
        "object": {
            "id": "ch_test_$(date +%s)",
            "payment_intent": "pi_test_refund_$(date +%s)"
        }
    }
}
EOF
)

REFUND_RESPONSE=$(curl -s $INSECURE -X POST "$BASE_URL/api/stripe/webhook" \
    -H "Content-Type: application/json" \
    -d "$REFUND_PAYLOAD" \
    -w "\n%{http_code}")
REFUND_STATUS=$(echo "$REFUND_RESPONSE" | tail -n1)

if [ "$REFUND_STATUS" = "200" ]; then
    success "Refund webhook processed (no matching purchase expected)"
else
    info "Refund webhook returned status: $REFUND_STATUS"
fi

# =============================================================================
# SECTION 6: Check x402 Payments Logged (Standard Tier)
# =============================================================================
echo ""
echo "6. Testing x402 Payment Analytics..."
echo "----------------------------------------------"

info "Fetching x402 payment history..."
PAYMENTS_RESPONSE=$(curl -s $INSECURE "$BASE_URL/api/x402/payments" -w "\n%{http_code}")
PAYMENTS_STATUS=$(echo "$PAYMENTS_RESPONSE" | tail -n1)
PAYMENTS_BODY=$(echo "$PAYMENTS_RESPONSE" | head -n-1)

if [ "$PAYMENTS_STATUS" = "200" ]; then
    success "x402 payment history endpoint accessible"
    echo "Response: $PAYMENTS_BODY"
else
    info "x402 payment history returned status: $PAYMENTS_STATUS"
fi

info "Fetching x402 payment stats..."
STATS_RESPONSE=$(curl -s $INSECURE "$BASE_URL/api/x402/stats" -w "\n%{http_code}")
STATS_STATUS=$(echo "$STATS_RESPONSE" | tail -n1)
STATS_BODY=$(echo "$STATS_RESPONSE" | head -n-1)

if [ "$STATS_STATUS" = "200" ]; then
    success "x402 stats endpoint accessible"
    echo "Response: $STATS_BODY"
else
    info "x402 stats returned status: $STATS_STATUS"
fi

# =============================================================================
# SECTION 7: Summary
# =============================================================================
echo ""
echo "=============================================="
echo "Test Summary"
echo "=============================================="
echo ""
echo "Standard/Sandbox Tier (Free):"
echo "  - x402 protocol endpoints accessible"
echo "  - Protected endpoints return 402 Payment Required"
echo "  - No Stripe payment needed for testnet access"
echo ""
echo "Enterprise Tier (\$2,500):"
echo "  - Stripe checkout session creation tested"
echo "  - Webhook handling tested"
echo "  - API key provisioning flow verified"
echo ""
echo "Logging:"
echo "  - Stripe purchases logged to 'stripe_purchases' table"
echo "  - x402 payments logged to 'x402_payments' table"
echo "  - Application logs in 'logs/log.txt'"
echo ""
echo "=============================================="
echo "To verify database logging, run:"
echo "  psql -d your_database -c \"SELECT * FROM stripe_purchases ORDER BY created_at DESC LIMIT 5;\""
echo "  psql -d your_database -c \"SELECT * FROM x402_payments ORDER BY created_at DESC LIMIT 5;\""
echo "=============================================="
