# AgentRails User Guide

This guide covers how to use the AgentRails REST APIs for managing autonomous AI agents and x402 payments.

---

## Table of Contents

1. [Getting Started](#getting-started)
2. [x402 Payment Protocol](#x402-payment-protocol)
3. [API Reference](#api-reference)
4. [Authentication](#authentication)
5. [Examples](#examples)

---

## Getting Started

### Prerequisites

- AgentRails API access (API key required for production)
- For testing: Use the sandbox at `https://sandbox.agentrails.io`

### Quick Start

1. Sign up for a free sandbox account at [agentrails.io](https://agentrails.io) or via the Signup API
2. Test with testnet USDC on Arc testnet, Base Sepolia, or Ethereum Sepolia
3. Upgrade to pay-as-you-go (0.5% per tx) or Pro ($49/mo) for mainnet access

---

## x402 Payment Protocol

AgentRails implements the [x402 protocol](https://x402.org) - an HTTP payment standard that enables AI agents to pay for API access using USDC.

### How x402 Works

```
┌─────────────┐                              ┌─────────────┐
│   AI Agent  │                              │  API Server │
└──────┬──────┘                              └──────┬──────┘
       │                                            │
       │  1. GET /api/resource                      │
       │ ─────────────────────────────────────────► │
       │                                            │
       │  2. 402 Payment Required                   │
       │     X-PAYMENT-REQUIRED: <base64>           │
       │ ◄───────────────────────────────────────── │
       │                                            │
       │  3. Sign EIP-3009 USDC authorization       │
       │                                            │
       │  4. GET /api/resource                      │
       │     X-PAYMENT: <signed_payload>            │
       │ ─────────────────────────────────────────► │
       │                                            │
       │  5. 200 OK + data                          │
       │ ◄───────────────────────────────────────── │
```

### x402 Endpoints

| Endpoint | Description |
|----------|-------------|
| `GET /api/x402/protected/analysis` | Paid endpoint - $0.01 USDC per request |
| `GET /api/x402/protected/data` | Micropayment endpoint - $0.001 USDC |
| `GET /api/x402/pricing` | Get pricing info for all endpoints |
| `POST /api/x402/facilitator/verify` | Verify payment payloads |
| `POST /api/x402/facilitator/settle` | Execute payment settlement on-chain |

---

## API Reference

Base URL: `https://api.agentrails.io`

### Agents Endpoints

#### List Agents
```http
GET /api/agents
X-API-Key: {your-api-key}
```

#### Create Agent
```http
POST /api/agents
X-API-Key: {your-api-key}
Content-Type: application/json

{
  "name": "Shopping Agent",
  "description": "Autonomous shopping assistant",
  "budget": 100.00
}
```

#### Get Agent Details
```http
GET /api/agents/{agentId}
X-API-Key: {your-api-key}
```

#### Delete Agent
```http
DELETE /api/agents/{agentId}
X-API-Key: {your-api-key}
```

#### Make Purchase (Agent Action)
```http
POST /api/agents/{agentId}/purchase
X-API-Key: {your-api-key}
Content-Type: application/json

{
  "amount": 25.00,
  "recipientAddress": "0x1234...",
  "description": "API subscription"
}
```

---

### Transactions Endpoints

#### Get Organization Transactions
```http
GET /api/agents/transactions
X-API-Key: {your-api-key}
```

**Query Parameters:**
- `limit` (optional): Maximum results (default: 100)

#### Get Wallet Balance
```http
GET /api/transactions/balance
```

#### Get Transaction Status
```http
GET /api/transactions/{txHash}
```

---

### x402 Payment Endpoints

#### Get Pricing
```http
GET /api/x402/pricing
```

#### Access Paid Resource
```http
GET /api/x402/protected/analysis
X-PAYMENT: <base64-encoded-payment-payload>
```

#### Verify Payment (Facilitator)
```http
POST /api/x402/facilitator/verify
Content-Type: application/json

{
  "x402Version": 1,
  "scheme": "exact",
  "network": "base-mainnet",
  "payload": {
    "signature": "0x...",
    "authorization": {
      "from": "0x...",
      "to": "0x...",
      "value": "10000",
      "validAfter": "0",
      "validBefore": "1234567890",
      "nonce": "0x..."
    }
  }
}
```

---

### Health & Status

#### Health Check
```http
GET /health
```

#### API Status
```http
GET /
```

---

## Authentication

AgentRails uses API key authentication for all requests.

Include your API key in the `X-API-Key` header:

```
X-API-Key: ar_live_abc123...
```

### Getting an API Key

**Option 1: Via API (Recommended)**
```bash
# Create free sandbox account
curl -X POST https://api.agentrails.io/api/signup \
  -H "Content-Type: application/json" \
  -d '{"email": "you@example.com"}'

# Returns: {"apiKey": "ar_test_...", "tier": "sandbox"}
```

**Option 2: Via Dashboard**
1. Go to [app.agentrails.io](https://app.agentrails.io)
2. Sign up with your email
3. Your API key will be displayed in the dashboard

**API Key Prefixes:**
- `ar_test_` - Testnet keys (sandbox tier)
- `ar_live_` - Mainnet keys (paid tiers)

---

## Examples

### Create an Agent and Make a Purchase

```bash
# Set your API key
API_KEY="ar_live_your_key_here"

# Create an agent
AGENT=$(curl -s -X POST https://api.agentrails.io/api/agents \
  -H "X-API-Key: $API_KEY" \
  -H "Content-Type: application/json" \
  -d '{"name":"My Agent","description":"Test agent","budget":100}')

AGENT_ID=$(echo $AGENT | jq -r '.id')

# Make a purchase
curl -X POST "https://api.agentrails.io/api/agents/$AGENT_ID/purchase" \
  -H "X-API-Key: $API_KEY" \
  -H "Content-Type: application/json" \
  -d '{"amount":10,"recipientAddress":"0x123...","description":"Test purchase"}'
```

### Access x402 Protected Resource

```bash
# First request returns 402 with payment requirements
curl -i https://api.agentrails.io/api/x402/protected/analysis

# Sign the payment and retry with X-PAYMENT header
curl https://api.agentrails.io/api/x402/protected/analysis \
  -H "X-PAYMENT: eyJ4NDAyVmVyc2lvbiI6MSwi..."
```

---

## Tiers

| Feature | Sandbox (Free) | Pay-as-you-go (0.5%) | Pro ($49/mo) | Enterprise ($2,500) |
|---------|----------------|----------------------|--------------|---------------------|
| Environment | Testnet only | Mainnet | Mainnet | Self-hosted |
| Transaction Fees | N/A | 0.5% per tx | 0% (unlimited) | 0% (unlimited) |
| Networks | Arc, Base, ETH testnets | All networks | All networks | All networks |
| Support | Community | Email | Priority | Priority + Implementation |
| Policy Engine | - | - | - | ✓ |
| Admin Dashboard | - | ✓ | ✓ | ✓ |
| Full Source Code | - | - | - | ✓ |

**Crossover point:** At $9,800/mo transaction volume, 0.5% = $49. Above that, Pro saves money.

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| 401 Unauthorized | Check API key is correct |
| 402 Payment Required | Include valid X-PAYMENT header |
| 403 Forbidden | Verify your tier includes this feature |
| 429 Rate Limited | Reduce request frequency |

---

## Support

- **Documentation**: [agentrails.io/docs](https://agentrails.io/docs)
- **Sandbox**: [sandbox.agentrails.io](https://sandbox.agentrails.io)
- **Issues**: [GitHub Issues](https://github.com/kmatthewsio/AgenticCommerce/issues)
