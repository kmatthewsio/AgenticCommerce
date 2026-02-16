# Swagger API Walkthrough — AgentRails Sandbox

> **Base URL:** `https://sandbox.agentrails.io`
> **Swagger UI:** [https://sandbox.agentrails.io/swagger](https://sandbox.agentrails.io/swagger)
> **OpenAPI Spec:** `https://sandbox.agentrails.io/swagger/v1/swagger.json`

This guide walks you through every endpoint group in the Sandbox API using Swagger UI. The sandbox is the free-tier entry point — no payment or API key required for most endpoints.

---

## Table of Contents

1. [Open Swagger UI](#1-open-swagger-ui)
2. [Health Check](#2-health-check)
3. [Sign Up for an Account](#3-sign-up-for-an-account)
4. [Check Account Status](#4-check-account-status)
5. [x402 Pricing (Public)](#5-x402-pricing-public)
6. [Hit a Protected Endpoint (402 Response)](#6-hit-a-protected-endpoint-402-response)
7. [Execute a Test Payment](#7-execute-a-test-payment)
8. [View Payment History](#8-view-payment-history)
9. [View Payment Stats](#9-view-payment-stats)
10. [Create an Agent](#10-create-an-agent)
11. [List Agents](#11-list-agents)
12. [Run an Agent](#12-run-an-agent)
13. [Agent Purchases](#13-agent-purchases)
14. [Trust API — Check, Register, Browse](#14-trust-api)
15. [Billing & Usage](#15-billing--usage)
16. [Blockchain & Wallet Endpoints](#16-blockchain--wallet-endpoints)
17. [x402 Facilitator Endpoints](#17-x402-facilitator-endpoints)
18. [x402 Example Endpoints (Attribute-Based)](#18-x402-example-endpoints)
19. [Stripe & Gumroad Webhooks](#19-stripe--gumroad-webhooks)
20. [Authentication Reference](#20-authentication-reference)

---

## 1. Open Swagger UI

1. Navigate to [https://sandbox.agentrails.io/swagger](https://sandbox.agentrails.io/swagger)
2. You'll see the interactive API explorer titled **"AgentRails Sandbox API v1"**
3. Each endpoint group is collapsible — click a group name to expand it
4. Click **"Try it out"** on any endpoint to send a live request

> **Tip:** Swagger runs against the live sandbox. All requests hit real endpoints with test data.

---

## 2. Health Check

**Endpoint:** `GET /health`

1. Expand the **Health** group
2. Click **Try it out** → **Execute**
3. Expected response: `200 OK` with `Healthy`

This confirms the API is running.

---

## 3. Sign Up for an Account

**Endpoint:** `POST /api/signup`

1. Expand the **Signup** group
2. Click `POST /api/signup` → **Try it out**
3. Edit the request body:

```json
{
  "email": "you@example.com",
  "password": "YourPassword123!"
}
```

4. Click **Execute**
5. Expected `200` response:

```json
{
  "success": true,
  "email": "you@example.com",
  "tier": "sandbox",
  "apiKey": "ar_test_abc123...",
  "environment": "testnet",
  "message": "Sandbox account created."
}
```

6. **Copy the `apiKey` value** — you'll need it for authenticated endpoints below.

> **Note:** Both `email` and `password` are required. Password must be at least 8 characters. You can log into the [Dashboard](https://app.agentrails.io) with these same credentials.

---

## 4. Check Account Status

**Endpoint:** `GET /api/signup/status`

1. Click `GET /api/signup/status` → **Try it out**
2. Add the query parameter `email` = your email
3. Click **Execute**
4. Response shows your current tier and API key prefixes

---

## 5. x402 Pricing (Public)

**Endpoint:** `GET /api/x402/pricing`

1. Expand the **X402** group
2. Click `GET /api/x402/pricing` → **Try it out** → **Execute**
3. Response (no auth needed):

```json
{
  "endpoints": [
    { "resource": "/api/x402/protected/analysis", "amountUsdc": 0.01 },
    { "resource": "/api/x402/protected/data", "amountUsdc": 0.001 }
  ],
  "supportedNetworks": [
    "arc-testnet", "base-sepolia", "ethereum-sepolia",
    "base-mainnet", "ethereum-mainnet"
  ],
  "payTo": "0x6255d8dd3f84ec460fc8b07db58ab06384a2f487"
}
```

This tells you what each protected endpoint costs and which blockchain networks are accepted.

---

## 6. Hit a Protected Endpoint (402 Response)

**Endpoint:** `GET /api/x402/protected/analysis`

1. Click `GET /api/x402/protected/analysis` → **Try it out** → **Execute**
2. Expected response: **`402 Payment Required`**
3. The response headers include `X-PAYMENT-REQUIRED` (base64-encoded) with:
   - Amount in USDC
   - Recipient wallet address
   - Supported networks
   - USDC contract addresses per network

> This is the x402 protocol in action. In production, an agent SDK reads this header and automatically signs a payment.

You can also try `GET /api/x402/protected/data` — same flow, but costs $0.001 instead of $0.01.

---

## 7. Execute a Test Payment

**Endpoint:** `POST /api/x402/test/execute-payment`

1. Click `POST /api/x402/test/execute-payment` → **Try it out**
2. Set the query parameter `amountUsdc` = `0.01`
3. Click **Execute**
4. Expected `200` response with a transaction hash and payment confirmation

> **Note:** This executes a real USDC transfer on Arc testnet using the platform's test wallet. No real money is involved.

### Other Test Endpoints

| Endpoint | What it does |
|----------|-------------|
| `GET /api/x402/test/wallet-status` | Shows Circle wallet config and connectivity |
| `GET /api/x402/test/generate-payload` | Generates a mock payment payload (for testing parsers) |
| `GET /api/x402/test/generate-signed-payload` | Generates a real cryptographic payload signed with a Hardhat test key |

---

## 8. View Payment History

**Endpoint:** `GET /api/x402/payments`

1. Click `GET /api/x402/payments` → **Try it out**
2. Optional filters:
   - `network` — e.g. `arc-testnet`
   - `status` — e.g. `settled`
   - `limit` — default 50, max 500
3. Click **Execute**
4. Response: array of payment objects with amount, status, network, payer address, transaction hash, timestamps

---

## 9. View Payment Stats

**Endpoint:** `GET /api/x402/stats`

1. Click `GET /api/x402/stats` → **Try it out** → **Execute**
2. Response includes:
   - Total payment count and USDC volume
   - Settlement success rate
   - Breakdown by network
   - Recent payment activity

---

## 10. Create an Agent

**Endpoint:** `POST /api/agents`

1. Expand the **Agents** group
2. Click `POST /api/agents` → **Try it out**
3. Request body:

```json
{
  "name": "My Test Agent",
  "description": "A test agent for exploring the API",
  "model": "gpt-4o",
  "budgetUsdc": 1.00
}
```

4. Click **Execute**
5. Response: the created agent object with an `id`

> **Save the agent `id`** — you'll need it for run/purchase/delete operations.

---

## 11. List Agents

**Endpoint:** `GET /api/agents`

1. Click `GET /api/agents` → **Try it out** → **Execute**
2. Returns all agents (or just your org's agents if authenticated)

### Dashboard View

**Endpoint:** `GET /api/agents/dashboard`

Returns agents with summary stats (total budget, amount spent, active count). Requires JWT auth.

---

## 12. Run an Agent

**Endpoint:** `POST /api/agents/{agentId}/run`

1. Click `POST /api/agents/{agentId}/run` → **Try it out**
2. Enter the agent ID from step 10
3. Request body:

```json
{
  "task": "Analyze the current price of USDC on Base"
}
```

4. Click **Execute**
5. The agent processes the task using its configured AI model and returns results

---

## 13. Agent Purchases

**Endpoint:** `POST /api/agents/{agentId}/purchase`

1. Enter the agent ID
2. Request body:

```json
{
  "serviceUrl": "https://sandbox.agentrails.io/api/x402/protected/analysis",
  "maxAmountUsdc": 0.05
}
```

3. Click **Execute**
4. The agent attempts to purchase access to the specified x402-protected endpoint

---

## 14. Trust API

### Check Trust Score

**Endpoint:** `GET /api/trust/check`

1. Expand the **Trust** group
2. Click `GET /api/trust/check` → **Try it out**
3. Set `serviceUrl` = `https://sandbox.agentrails.io/api/x402/protected/analysis`
4. Click **Execute**
5. Response includes trust score (`high`, `medium`, `low`, or `unknown`)

### Register a Service

**Endpoint:** `POST /api/trust/register`

```json
{
  "serviceUrl": "https://your-api.com/endpoint",
  "name": "Your API Name",
  "description": "What your API does",
  "ownerWallet": "0x...",
  "priceUsdc": 0.01
}
```

### Browse Registry

**Endpoint:** `GET /api/trust/registry`

- Optional filter: `verified=true` to show only verified services
- Returns all registered x402 services

### Get Payment Stats for a Service

**Endpoint:** `GET /api/trust/stats`

- Set `serviceUrl` query parameter
- Returns payment count, volume, and success rate for that service

---

## 15. Billing & Usage

### Get Usage by API Key

**Endpoint:** `GET /api/billing/usage`

1. Expand the **Billing** group
2. Click `GET /api/billing/usage` → **Try it out**
3. Set `days` = `30`
4. Add your API key in the `X-Api-Key` header (use the Authorize button at the top of Swagger)
5. Response:

```json
{
  "organization": { "id": "...", "tier": "sandbox" },
  "period": { "from": "2026-01-17", "to": "2026-02-16", "days": 30 },
  "usage": {
    "transactionCount": 5,
    "totalVolumeUsdc": 0.05,
    "totalFeesUsd": 0.00,
    "billedFeesUsd": 0.00,
    "unbilledFeesUsd": 0.00
  }
}
```

### Get Usage by Email

**Endpoint:** `GET /api/billing/usage/{email}`

- Enter the email address associated with the account
- Returns the same usage summary

---

## 16. Blockchain & Wallet Endpoints

### Check Blockchain Status

**Endpoint:** `GET /api/transactions/status`

- Returns whether the Arc blockchain connection is active

### Get Wallet Balance

| Endpoint | Description |
|----------|-------------|
| `GET /api/transactions/balance` | Arc testnet wallet balance |
| `GET /api/transactions/balance/total` | Total USDC across all chains (Circle Gateway) |
| `GET /api/transactions/balance/by-chain` | Balance breakdown per blockchain |

### Send USDC

**Endpoint:** `POST /api/transactions/send`

```json
{
  "toAddress": "0x...",
  "amount": 0.01
}
```

### Look Up a Transaction

| Endpoint | Description |
|----------|-------------|
| `GET /api/transactions/{txHash}` | Transaction details |
| `GET /api/transactions/{txHash}/receipt` | Transaction receipt/confirmation |

### Supported Chains

**Endpoint:** `GET /api/transactions/chains`

Returns all blockchains supported by Circle Gateway.

---

## 17. x402 Facilitator Endpoints

These are server-to-server endpoints for verifying and settling x402 payments programmatically.

### Verify Payment

**Endpoint:** `POST /api/x402/facilitator/verify`

Submit a payment payload to verify the EIP-3009 signature without settling.

### Settle Payment

**Endpoint:** `POST /api/x402/facilitator/settle`

Submit a verified payment payload to execute on-chain settlement.

---

## 18. x402 Example Endpoints

These demonstrate the `[X402Payment]` attribute pattern for protecting endpoints:

| Endpoint | Cost | Description |
|----------|------|-------------|
| `GET /api/x402-example/free` | Free | No payment required (baseline) |
| `GET /api/x402-example/simple` | $0.01 | Basic paid endpoint |
| `GET /api/x402-example/micro` | $0.001 | Micropayment example |
| `POST /api/x402-example/premium` | $0.10 | Premium operation |
| `GET /api/x402-example/multichain` | $0.05 | Multi-network (Arc + Base + Ethereum) |

Try hitting `/simple` without a payment header to see the 402 response, then compare with `/free`.

---

## 19. Stripe & Gumroad Webhooks

These endpoints handle incoming webhooks and are **not meant to be called manually** from Swagger:

| Endpoint | Purpose |
|----------|---------|
| `POST /api/stripe/webhook` | Handles Stripe checkout and refund events |
| `POST /api/stripe/create-checkout-session` | Creates a Stripe Checkout session for paid tiers |
| `POST /api/gumroad/webhook` | Handles Gumroad purchase/refund notifications |
| `GET /api/gumroad/verify/{licenseKey}` | Verify a Gumroad license key |

### Test Creating a Checkout Session

**Endpoint:** `POST /api/stripe/create-checkout-session`

```json
{
  "email": "you@example.com",
  "tier": "pro"
}
```

Tiers: `payg` (0.5% per tx), `pro` ($49/mo), `startup` ($500), `enterprise` ($2,500)

---

## 20. Authentication Reference

The sandbox API supports multiple auth methods. Most endpoints work without auth for easy testing.

### No Auth (Most Endpoints)

The majority of sandbox endpoints are open. Just click **Try it out** and go.

### API Key Header

For endpoints that filter by organization (agents, billing), pass your API key:

1. Click the **Authorize** button (lock icon) at the top of Swagger UI
2. Enter your API key in the `X-API-Key` field
3. Click **Authorize**
4. All subsequent requests will include the header

Or set it per-request: `X-API-Key: ar_test_YOUR_KEY`

### JWT Bearer Token

For dashboard endpoints:

1. Call `POST /api/signup` to create an account (if you haven't)
2. Use the credentials to log into [app.agentrails.io](https://app.agentrails.io) and extract the JWT, or use the enterprise API's `/api/auth/login` endpoint
3. Click **Authorize** in Swagger → enter `Bearer YOUR_JWT_TOKEN`

### x402 Payment Header

For protected endpoints (`/api/x402/protected/*`):

- Header: `X-PAYMENT` containing a base64-encoded EIP-3009 signed authorization
- In practice, agent SDKs (LangChain, CrewAI) handle this automatically
- For manual testing, use the test endpoints instead (`/api/x402/test/execute-payment`)

---

## Recommended Walkthrough Order

If you're exploring the API for the first time, follow this sequence:

| Step | Endpoint | Why |
|------|----------|-----|
| 1 | `GET /health` | Confirm API is running |
| 2 | `POST /api/signup` | Create your account and get an API key |
| 3 | `GET /api/x402/pricing` | See what paid endpoints cost |
| 4 | `GET /api/x402/protected/analysis` | See a 402 response in action |
| 5 | `POST /api/x402/test/execute-payment?amountUsdc=0.01` | Execute a test payment |
| 6 | `GET /api/x402/payments` | Confirm payment was recorded |
| 7 | `GET /api/x402/stats` | See aggregate stats |
| 8 | `POST /api/agents` | Create an agent |
| 9 | `GET /api/agents` | List your agents |
| 10 | `GET /api/trust/check?serviceUrl=...` | Check a service's trust score |
| 11 | `GET /api/billing/usage?days=30` | Check your usage (with API key) |
| 12 | Open [app.agentrails.io](https://app.agentrails.io) | See everything in the dashboard |

---

## Environment Details

| Property | Value |
|----------|-------|
| **API Base URL** | `https://sandbox.agentrails.io` |
| **Swagger UI** | `https://sandbox.agentrails.io/swagger` |
| **Dashboard** | `https://app.agentrails.io` |
| **Environment** | Testnet (no real money) |
| **USDC** | Test USDC on Arc testnet |
| **API Key Prefix** | `ar_test_*` |
| **Networks** | arc-testnet, base-sepolia, ethereum-sepolia, base-mainnet, ethereum-mainnet |
