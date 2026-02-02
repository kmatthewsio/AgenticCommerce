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

1. Purchase a Startup or Enterprise license at [agentrails.io](https://agentrails.io)
2. Receive your API key via email
3. Start making API calls with your key

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

1. Purchase a license at [agentrails.io](https://agentrails.io)
2. Your API key will be emailed to you
3. Store the key securely - it cannot be retrieved again

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

| Feature | Sandbox (Free) | Startup ($500) | Enterprise ($2,500) |
|---------|----------------|----------------|---------------------|
| x402 Protocol | Test only | Production | Production |
| API Access | Limited | Full | Full |
| Agents | 1 | 10 | Unlimited |
| Support | Community | Email | Priority |
| Policy Engine | - | - | ✓ |
| Admin Dashboard | - | - | ✓ |
| Audit Logging | - | - | ✓ |

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
