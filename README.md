# Autonomous Agent Commerce Platform

> AI agents that research, decide, and execute USDC payments autonomously on Arc blockchain

Built with Circle Developer Controlled Wallets, Arc blockchain, and OpenAI GPT-4o for fully autonomous commerce.

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Circle](https://img.shields.io/badge/Circle-API-00D395)](https://developers.circle.com/)
[![Arc](https://img.shields.io/badge/Arc-Blockchain-4A90E2)](https://www.circle.com/en/pressroom/circle-announces-arc)
[![OpenAI](https://img.shields.io/badge/OpenAI-GPT--4o-412991?logo=openai)](https://openai.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791?logo=postgresql)](https://www.postgresql.org/)
[![x402](https://img.shields.io/badge/x402-V2%20Spec-FF6B35)](https://www.x402.org/)

## What This Does

Autonomous AI agents that can:
- **Research options** using built-in knowledge bases
- **Make informed decisions** via GPT-4o reasoning
- **Execute USDC payments** on Arc blockchain
- **Manage budgets** with constraint enforcement
- **Settle instantly** with sub-second finality
- **Persist forever** with PostgreSQL database
- **Pay for APIs** using x402 protocol (HTTP 402 payments)

**No human intervention required. Built for the institutional settlement model.**

## x402 Payment Protocol (V2 Spec-Compliant)

This platform implements the [x402 protocol](https://www.x402.org/) - an HTTP payment standard by Coinbase/Cloudflare that enables AI agents to pay for API access using cryptocurrency.

### How x402 Works

```
┌─────────────┐                              ┌─────────────┐
│   AI Agent  │                              │  API Server │
└──────┬──────┘                              └──────┬──────┘
       │                                            │
       │  1. GET /api/x402/protected/analysis       │
       │ ─────────────────────────────────────────► │
       │                                            │
       │  2. 402 Payment Required                   │
       │     X-PAYMENT-REQUIRED: <base64>           │
       │ ◄───────────────────────────────────────── │
       │                                            │
       │  3. Sign EIP-3009 authorization            │
       │     (transferWithAuthorization)            │
       │                                            │
       │  4. GET /api/x402/protected/analysis       │
       │     X-PAYMENT: <signed_payload>            │
       │ ─────────────────────────────────────────► │
       │                                            │
       │  5. Verify signature, settle on-chain      │
       │                                            │
       │  6. 200 OK + X-PAYMENT-RESPONSE            │
       │     { paid content }                       │
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
| `GET /api/x402/payments` | Payment history with filtering |
| `GET /api/x402/stats` | Aggregated payment statistics |

### x402 Example Flow

**Step 1: Request paid resource (no payment)**
```bash
curl https://your-api.com/api/x402/protected/analysis

# Response: 402 Payment Required
# Header: X-PAYMENT-REQUIRED: eyJ4NDAyVmVyc2lvbiI6Miwi...
{
  "x402Version": 2,
  "accepts": [{
    "scheme": "exact",
    "network": "arc-testnet",
    "maxAmountRequired": "10000",
    "resource": "/api/x402/protected/analysis",
    "description": "AI Analysis API - $0.01 per request",
    "payTo": "0x6255d8dd3f84ec460fc8b07db58ab06384a2f487"
  }]
}
```

**Step 2: Agent signs payment and retries**
```bash
curl https://your-api.com/api/x402/protected/analysis \
  -H "X-PAYMENT: eyJ4NDAyVmVyc2lvbiI6Miwic2NoZW1lIjoi..."

# Response: 200 OK
# Header: X-PAYMENT-RESPONSE: {"success":true,"transactionHash":"..."}
{
  "result": "AI analysis complete!",
  "analysis": "Premium AI-powered analysis data",
  "costUsdc": 0.01,
  "transactionHash": "58026c24-3874-55e9-8e76-6ab6d50fb7d8"
}
```

### Test Endpoints (Development)

| Endpoint | Description |
|----------|-------------|
| `GET /api/x402/test/wallet-status` | Check Circle wallet configuration |
| `GET /api/x402/test/generate-payload` | Generate test payloads for facilitator |
| `POST /api/x402/test/execute-payment` | Execute real payment on Arc testnet |

### x402 Payment Persistence

All x402 payments are persisted to PostgreSQL:

```sql
SELECT * FROM x402_payments;

-- Example record:
-- payment_id: x402_abc123_639039159723714890
-- resource: /api/x402/protected/analysis
-- network: arc-testnet
-- amount_usdc: 0.01
-- payer_address: 0xAgent...
-- recipient_address: 0xMerchant...
-- transaction_hash: 58026c24-3874-55e9-8e76-6ab6d50fb7d8
-- status: Settled
-- created_at: 2026-01-13T15:46:12Z
```

## Key Features

### Autonomous Decision-Making
- AI-powered research and analysis
- Budget validation and enforcement
- Risk assessment and strategy planning
- Multi-step reasoning with tool use

### Circle + Arc Integration
- Developer Controlled Wallets for agent custody
- Native USDC settlement on Arc blockchain
- Sub-second transaction finality (<1s)
- Predictable USDC-based gas fees
- No volatile gas tokens required

### x402 Facilitator Service
- Spec-compliant V2 implementation
- Payment verification and settlement
- EIP-3009 transferWithAuthorization support
- Multi-network support (Arc, Base, Ethereum)
- Payment history and analytics

### Production-Ready Database
- PostgreSQL persistence with EF Core
- Agents survive application restarts
- Complete transaction history tracking
- x402 payment audit trail
- Relational data model with migrations

## Quick Start

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (for PostgreSQL)
- [Circle Developer Account](https://developers.circle.com/)
- [OpenAI API Key](https://platform.openai.com/)

### 1. Clone the Repository
```bash
git clone https://github.com/kmatthewsio/AgenticCommerce.git
cd AgenticCommerce
```

### 2. Start PostgreSQL Database
```bash
docker run --name agenticcommerce-db \
  -e POSTGRES_PASSWORD=dev_password_change_in_prod \
  -e POSTGRES_DB=agenticcommerce \
  -p 5432:5432 \
  -d postgres:16
```

### 3. Configure Secrets

**Option A: User Secrets (Recommended for Development)**
```bash
cd src/AgenticCommerce.API

# Circle Configuration
dotnet user-secrets set "Circle:ApiKey" "your-circle-api-key"
dotnet user-secrets set "Circle:EntitySecret" "your-entity-secret-hex"
dotnet user-secrets set "Circle:WalletAddress" "0xYourWalletAddress"
dotnet user-secrets set "Circle:WalletId" "your-wallet-id"

# OpenAI Configuration
dotnet user-secrets set "OpenAI:ApiKey" "your-openai-api-key"
dotnet user-secrets set "OpenAI:Model" "gpt-4o"
```

**Option B: Environment Variables (Production)**
```bash
# Use double underscore for nested config
export Circle__ApiKey="your-circle-api-key"
export Circle__EntitySecret="your-entity-secret-hex"
export Circle__WalletAddress="0xYourWalletAddress"
export Circle__WalletId="your-wallet-id"
export OpenAI__ApiKey="your-openai-api-key"
```

### 4. Run the Application
```bash
cd src/AgenticCommerce.API
dotnet run --launch-profile https
```

Navigate to `https://localhost:7098/swagger` to explore the API.

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                  AgenticCommerce.API                         │
│         (REST API + Swagger + x402 Controllers)              │
└─────────────────────────┬───────────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────────┐
│              AgenticCommerce.Infrastructure                   │
│                                                               │
│  ┌──────────────────┐    ┌────────────────────────────────┐ │
│  │  Agent Service   │    │     x402 Payment Service       │ │
│  │  • Lifecycle     │    │     • Verify payments          │ │
│  │  • AI Execution  │    │     • Settle on-chain          │ │
│  │  • Budget Mgmt   │    │     • Payment persistence      │ │
│  └──────────────────┘    └────────────────────────────────┘ │
│                                                               │
│  ┌──────────────────┐    ┌────────────────────────────────┐ │
│  │  Circle/Arc      │    │       Database (EF Core)       │ │
│  │  • ArcClient     │    │       • Agents                 │ │
│  │  • USDC Transfers│    │       • Transactions           │ │
│  │  • Wallet API    │    │       • x402 Payments          │ │
│  └──────────────────┘    └────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────────┐
│                  AgenticCommerce.Core                         │
│           (Domain Models + Interfaces + x402 Spec)           │
└─────────────────────────────────────────────────────────────┘
```

## Tech Stack

- **Backend:** ASP.NET Core 8.0 / C#
- **Database:** PostgreSQL 16 + Entity Framework Core 8
- **Blockchain:** Circle Developer Controlled Wallets + Arc
- **AI:** OpenAI GPT-4o + Microsoft Semantic Kernel 1.30.0
- **Payments:** x402 V2 Protocol (EIP-3009)
- **API:** REST with OpenAPI/Swagger documentation

## Use Cases

### x402 API Monetization
Monetize your APIs with micropayments. AI agents pay per request automatically.

### Corporate Procurement
AI agents that research vendors, compare pricing, and execute purchases within approved budgets.

### x402 Facilitator Service
Offer payment verification and settlement as a service for other API providers.

### Autonomous Treasury
Agents that manage corporate funds, execute payments, and maintain budget compliance.

## Roadmap

### Phase 1: Core Infrastructure
- [x] Circle API integration
- [x] Arc blockchain settlement
- [x] AI agent reasoning (GPT-4o)
- [x] Autonomous execution
- [x] Budget management

### Phase 2: Production Database
- [x] PostgreSQL with Docker
- [x] Entity Framework Core migrations
- [x] Agent persistence
- [x] Transaction history tracking

### Phase 3: x402 V2 Protocol
- [x] Spec-compliant implementation
- [x] Payment verification (EIP-3009)
- [x] On-chain settlement via Arc
- [x] Facilitator endpoints (verify/settle)
- [x] Payment persistence & analytics
- [x] Test endpoints for development

### Phase 4: SaaS Features (Next)
- [ ] Multi-tenant support
- [ ] API key management
- [ ] Usage-based billing
- [ ] Merchant onboarding
- [ ] Analytics dashboard

### Phase 5: Agent Auto-Pay
- [ ] Agent detects 402 responses
- [ ] Automatic payment signing
- [ ] Full autonomous pay-per-call

## Security

- API keys encrypted at rest
- RSA-OAEP-SHA256 for Circle authentication
- Budget constraints enforced cryptographically
- EIP-3009 signature verification
- Database with proper foreign keys and constraints
- Comprehensive audit logging

## Performance

- **Transaction Finality:** <1 second (Arc deterministic finality)
- **Agent Decision Time:** 5-15 seconds (GPT-4o reasoning)
- **x402 Verification:** <50ms
- **Database Queries:** <50ms (indexed PostgreSQL)

## Contributing

This is a personal project demonstrating autonomous agent commerce and x402 payment facilitation. Feedback and suggestions welcome!

## License

MIT License - See [LICENSE](LICENSE) file for details

## Acknowledgments

Built with:
- [Circle](https://www.circle.com/) - USDC infrastructure and Arc blockchain
- [OpenAI](https://openai.com/) - GPT-4o for agent reasoning
- [x402.org](https://www.x402.org/) - Payment protocol specification
- [Coinbase](https://github.com/coinbase/x402) - x402 reference implementation
- [PostgreSQL](https://www.postgresql.org/) - Production-grade database

## Contact

Questions? Reach out:
- GitHub Issues: [Create an issue](https://github.com/kmatthewsio/AgenticCommerce/issues)
- Twitter: https://x.com/kevlondonbtc

---

**Built in January 2026 using AI-assisted development.**

**Autonomous commerce + x402 payments on institutional settlement infrastructure.**
