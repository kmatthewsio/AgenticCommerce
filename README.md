# Autonomous Agent Commerce Platform

> AI agents that research, decide, and execute USDC payments autonomously on Arc blockchain

Built with Circle Developer Controlled Wallets, Arc blockchain, and OpenAI GPT-4o for fully autonomous commerce.

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Circle](https://img.shields.io/badge/Circle-API-00D395)](https://developers.circle.com/)
[![Arc](https://img.shields.io/badge/Arc-Blockchain-4A90E2)](https://www.circle.com/en/pressroom/circle-announces-arc)
[![OpenAI](https://img.shields.io/badge/OpenAI-GPT--4o-412991?logo=openai)](https://openai.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791?logo=postgresql)](https://www.postgresql.org/)

## 🎯 What This Does

Autonomous AI agents that can:
- 🧠 **Research options** using built-in knowledge bases
- 💡 **Make informed decisions** via GPT-4o reasoning
- 💰 **Execute USDC payments** on Arc blockchain
- 📊 **Manage budgets** with constraint enforcement
- ⚡ **Settle instantly** with sub-second finality
- 💾 **Persist forever** with PostgreSQL database

**No human intervention required. Built for the institutional settlement model.**

## ✨ Key Features

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

### Production-Ready Database
- PostgreSQL persistence with EF Core
- Agents survive application restarts
- Complete transaction history tracking
- Relational data model with migrations

### Production-Ready API
- RESTful endpoints with OpenAPI/Swagger
- Agent lifecycle management
- Transaction tracking and receipts
- Budget monitoring
- Comprehensive error handling

### x402 Payment Protocol
- Pay-per-call micropayments for APIs
- Payment requirement generation (402 responses)
- Payment proof verification
- Usage-based autonomous spending
- Demo endpoints for testing

## 💳 x402 Payment Protocol Example

### Step 1: API Returns Payment Requirement
```bash
GET /api/x402-demo/ai-analysis

Response: 402 Payment Required
{
  "paymentId": "pay_abc123",
  "amount": 0.01,
  "recipientAddress": "0x...",
  "description": "AI Analysis API Call - $0.01 per request",
  "expiresAt": "2026-01-05T17:05:00Z"
}
```

### Step 2: Agent Makes Payment
```bash
POST /api/agents/{agentId}/purchases
{
  "recipientAddress": "0x...",
  "amount": 0.01,
  "description": "x402 payment"
}

Response:
{
  "transactionId": "2ea0ed60-99b5-54ea-944f-ccf66cb4564d",
  "success": true
}
```

### Step 3: Verify Payment
```bash
POST /api/x402-demo/verify-payment
{
  "paymentId": "pay_abc123",
  "transactionId": "2ea0ed60-99b5-54ea-944f-ccf66cb4564d",
  "amount": 0.01
}

Response:
{
  "isValid": true,
  "verifiedAt": "2026-01-05T16:00:00Z"
}
```

### Step 4: Get API Response
```bash
GET /api/x402-demo/ai-analysis?paymentProof=verified

Response: 200 OK
{
  "result": "AI analysis complete!",
  "cost": 0.01
}
```

**Enables true pay-per-call autonomous commerce.**

## 🚀 Quick Start

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

Create `src/AgenticCommerce.API/appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=agenticcommerce;Username=postgres;Password=dev_password_change_in_prod"
  },
  "Circle": {
    "ApiKey": "your-circle-api-key",
    "EntitySecret": "your-entity-secret",
    "WalletAddress": "your-wallet-address",
    "WalletId": "your-wallet-id",
    "WalletsApiUrl": "https://api.circle.com/v1/w3s",
    "GatewayApiUrl": "https://gateway-api-testnet.circle.com/v1"
  },
  "OpenAI": {
    "ApiKey": "your-openai-api-key",
    "Model": "gpt-4o"
  }
}
```

> **Note:** Never commit this file. It's already in `.gitignore`.

### 4. Apply Database Migrations
```bash
cd src/AgenticCommerce.API
dotnet ef database update --project ../AgenticCommerce.Infrastructure --startup-project .
```

### 5. Run the Application
```bash
dotnet restore
dotnet build
dotnet run
```

Navigate to `https://localhost:7098/swagger` to explore the API.

## 📖 Usage Examples

### Create an Autonomous Agent
```bash
POST /api/agents
{
  "name": "Research Agent",
  "description": "Autonomous AI agent for API research and procurement",
  "budget": 100.0,
  "capabilities": ["research", "analysis", "payments"]
}
```

### Run Autonomous Task
```bash
POST /api/agents/{agentId}/run
{
  "task": "Research AI API providers under $50 and buy the best option for $1 as a test"
}
```

**The agent will:**
1. Research AI API providers autonomously
2. Compare pricing and features
3. Make an informed decision
4. Validate against budget constraints
5. Execute USDC payment on Arc blockchain
6. Save transaction to database
7. Return transaction proof

### Example Response
```json
{
  "success": true,
  "result": "Research complete. Selected Google Gemini 1.5 Flash ($0.075/1M tokens).\n\nPurchase executed:\n- Amount: $1 USDC\n- Transaction ID: 62c6bf40-c7a5-5e84-9fb9-f6e8fbb45630\n- Recipient: 0x6255d8dd3f84ec460fc8b07db58ab06384a2f487",
  "amountSpent": 1.0,
  "transactionIds": ["62c6bf40-c7a5-5e84-9fb9-f6e8fbb45630"],
  "completedAt": "2026-01-05T15:54:40Z"
}
```

**Transaction proof on Arc testnet:** ✅

**Agent persists across restarts:** ✅

## 🏗️ Architecture
```
┌─────────────────────────────────────────────────┐
│           AgenticCommerce.API                    │
│  (REST API + Swagger + Controllers)             │
└─────────────────┬───────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────┐
│      AgenticCommerce.Infrastructure              │
│                                                   │
│  ┌──────────────┐  ┌────────────────────────┐  │
│  │ AgentService │  │ AI Reasoning Engine    │  │
│  │              │  │ (Semantic Kernel +     │  │
│  │ • Lifecycle  │  │  OpenAI GPT-4o)        │  │
│  │ • Execution  │  └────────────────────────┘  │
│  │ • Budget     │                               │
│  └──────────────┘  ┌────────────────────────┐  │
│                    │ Circle Integration      │  │
│  ┌──────────────┐  │ • Arc Client            │  │
│  │ Database     │  │ • Gateway Client        │  │
│  │ • PostgreSQL │  │ • USDC Transactions     │  │
│  │ • EF Core    │  └────────────────────────┘  │
│  │ • Migrations │                               │
│  └──────────────┘                               │
└───────────────────────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────┐
│         AgenticCommerce.Core                     │
│  (Domain Models + Interfaces)                    │
└──────────────────────────────────────────────────┘
```

## 🛠️ Tech Stack

- **Backend:** ASP.NET Core 8.0 / C#
- **Database:** PostgreSQL 16 + Entity Framework Core 8
- **Blockchain:** Circle Developer Controlled Wallets + Arc
- **AI:** OpenAI GPT-4o + Microsoft Semantic Kernel 1.30.0
- **Authentication:** RSA-OAEP-SHA256 encryption
- **API:** REST with OpenAPI/Swagger documentation

## 🎯 Use Cases

### Corporate Procurement
AI agents that research vendors, compare pricing, and execute purchases within approved budgets.

### API Credit Management
Agents that monitor usage, optimize provider selection, and automatically purchase credits.

### Autonomous Treasury
Agents that manage corporate funds, execute payments, and maintain budget compliance.

### Trading Bots
Autonomous agents that analyze markets and execute trades with built-in risk management.

## 🗺️ Roadmap

### ✅ Phase 1: Core Infrastructure (Complete)
- [x] Circle API integration
- [x] Arc blockchain settlement
- [x] AI agent reasoning (GPT-4o)
- [x] Autonomous execution
- [x] Budget management
- [x] Transaction tracking

### ✅ Phase 2: Production Database (Complete)
- [x] PostgreSQL with Docker
- [x] Entity Framework Core migrations
- [x] Agent persistence across restarts
- [x] Transaction history tracking
- [x] Relational data model

### ✅ Phase 3: x402 Payment Protocol (Complete)
- [x] Payment requirement generation (402 responses)
- [x] Payment proof verification
- [x] Micropayment tracking
- [x] Pay-per-call API infrastructure
- [x] Demo x402-enabled endpoint

### 🚧 Phase 4: Agent Auto-Pay (Next)
- [ ] Agent detects 402 responses automatically
- [ ] Agent pays and retries with proof
- [ ] Full autonomous pay-per-call workflow
- [ ] Usage-based spending tracking

### 📋 Phase 5: Production Features (Planned)
- [ ] Enhanced error handling and retry logic
- [ ] Rate limiting and quotas
- [ ] Webhook notifications
- [ ] Multi-agent orchestration
- [ ] Analytics dashboard

### 🎨 Phase 6: Developer Experience (Planned)
- [ ] Web dashboard (React/Next.js)
- [ ] Agent templates and presets
- [ ] Developer SDKs (Python, TypeScript)
- [ ] Comprehensive documentation

## 🔐 Security

- API keys encrypted at rest
- RSA-OAEP-SHA256 for Circle authentication
- Budget constraints enforced cryptographically
- Transaction validation before execution
- Database with proper foreign keys and constraints
- Comprehensive audit logging

## 💡 Design Philosophy

**Built on the institutional settlement model:**
- Business logic stays private (in your API)
- Blockchain is for settlement only (like BlackRock's BUIDL)
- Arc provides what institutions need: deterministic finality, known validators, stable gas
- No unnecessary smart contracts - just fast, certain USDC transfers

**Mirrors Circle's StableFX approach:**
- Offchain decision-making (AI reasoning, budget validation)
- Onchain settlement (USDC transfers with instant finality)
- Same infrastructure banks use, but for autonomous agents

## 📊 Performance

- **Transaction Finality:** <1 second (Arc deterministic finality)
- **Agent Decision Time:** 5-15 seconds (GPT-4o reasoning)
- **Database Queries:** <50ms (indexed PostgreSQL)
- **API Response Time:** <100ms (without AI execution)

## 🤝 Contributing

This is a personal project demonstrating autonomous agent commerce on institutional settlement infrastructure. Feedback and suggestions welcome!

## 📄 License

MIT License - See [LICENSE](LICENSE) file for details

## 🙏 Acknowledgments

Built with:
- [Circle](https://www.circle.com/) - USDC infrastructure and Arc blockchain
- [OpenAI](https://openai.com/) - GPT-4o for agent reasoning
- [Microsoft Semantic Kernel](https://github.com/microsoft/semantic-kernel) - AI orchestration
- [PostgreSQL](https://www.postgresql.org/) - Production-grade database

Special thanks to the Circle Developer Relations team for Arc and Gateway documentation.

## 📞 Contact

Questions? Reach out:
- GitHub Issues: [Create an issue](https://github.com/yourusername/AgenticCommerce/issues)
- Twitter: https://x.com/kevlondonbtc

---

**Built in January 2026 using AI-assisted development.**

**Autonomous commerce on institutional settlement infrastructure.** 🚀

**Phase 1 + Phase 2 complete. Agents persist. Transactions finalized. Production-ready.**