# Agentic Commerce Platform

Autonomous AI agents that can manage budgets and execute blockchain transactions.

## 🎯 What It Does

- **Create AI Agents** with spending budgets
- **Autonomous Purchases** on Arc blockchain via Circle API
- **Budget Management** - agents track spending limits
- **Real-time Transactions** - sub-second settlement on Arc
- **Full REST API** - Swagger documentation included

### First Autonomous Agent Purchase

- **Transaction:** `b8f15a06-fe88-501b-9d23-4c9ae57fe130`
- **Amount:** 1 USDC
- **Network:** Arc Testnet
- **Status:** ✅ Complete

## 🚀 Tech Stack

- **Backend:** ASP.NET Core 8.0
- **Blockchain:** Circle Developer Controlled Wallets + Arc
- **AI:** OpenAI GPT-4o + Microsoft Semantic Kernel
- **Authentication:** RSA-OAEP-SHA256 encryption
- **API:** REST with Swagger/OpenAPI

## 🏗️ Architecture
```
API Layer (Controllers)
    ↓
Agent Service (Budget Management + AI Reasoning)
    ↓
Arc Client (Circle API Integration)
    ↓
Circle Developer Controlled Wallets
    ↓
Arc Blockchain (USDC Settlement)
```

## 📦 Quick Start

### Prerequisites

- .NET 8.0 SDK
- Circle Developer Account
- OpenAI API Key
- Arc Testnet USDC

### Setup

1. **Clone the repo**
```bash
   git clone https://github.com/kmatthewsio/AgenticCommerce.git
   cd AgenticCommerce
```

2. **Copy configuration template**
```bash
   cp src/AgenticCommerce.API/Properties/launchSettings.TEMPLATE.json src/AgenticCommerce.API/Properties/launchSettings.json
```

3. **Configure credentials** in `launchSettings.json`:
```json
   {
     "Circle__ApiKey": "YOUR_CIRCLE_API_KEY",
     "Circle__EntitySecret": "YOUR_ENTITY_SECRET",
     "Circle__WalletAddress": "YOUR_WALLET_ADDRESS",
     "Circle__WalletId": "YOUR_WALLET_ID",
     "OpenAI__ApiKey": "YOUR_OPENAI_API_KEY"
   }
```

4. **Run the application**
```bash
   cd src/AgenticCommerce.API
   dotnet run --launch-profile https
```

5. **Open Swagger UI**
```
   https://localhost:7098/swagger
```

## 🎮 Usage

### Create an Agent
```bash
POST /api/agents
{
  "name": "Shopping Agent",
  "budget": 100.00,
  "capabilities": ["purchase", "research"]
}
```

### Run Agent Task (AI Reasoning)
```bash
POST /api/agents/{agentId}/run
{
  "task": "Research the best AI API providers under $50 and recommend one"
}
```

### Make a Purchase
```bash
POST /api/agents/{agentId}/purchase
{
  "recipientAddress": "0x...",
  "amount": 1.0,
  "description": "Autonomous purchase"
}
```

### Check Agent Status
```bash
GET /api/agents/{agentId}/info
```

## 📊 API Endpoints

### Agents
- `POST /api/agents` - Create agent
- `GET /api/agents` - List all agents
- `GET /api/agents/{id}` - Get agent details
- `GET /api/agents/{id}/info` - Get agent summary
- `POST /api/agents/{id}/run` - Run agent task (AI reasoning)
- `POST /api/agents/{id}/purchase` - Execute purchase
- `DELETE /api/agents/{id}` - Delete agent

### Transactions
- `GET /api/transactions/balance` - Check wallet balance
- `GET /api/transactions/balance/total` - Total balance across all chains (Gateway)
- `GET /api/transactions/balance/by-chain` - Balance breakdown by chain
- `POST /api/transactions/send` - Send USDC
- `GET /api/transactions/{id}` - Get transaction details
- `GET /api/transactions/{id}/receipt` - Get transaction receipt
- `GET /api/transactions/status` - Check Arc connection
- `GET /api/transactions/chains` - Get supported blockchains

## 🌟 Features

### AI-Powered Reasoning
Agents use OpenAI GPT-4o with Microsoft Semantic Kernel to:
- Research API providers and pricing
- Compare options based on budget
- Calculate costs and ROI
- Make informed recommendations
- Explain their reasoning

### Budget Management
- Agents have allocated budgets
- Track spending in real-time
- Prevent overspending
- Transaction history per agent

### Blockchain Integration
- Circle Developer Controlled Wallets
- Arc blockchain for instant settlement
- Sub-second USDC transfers
- RSA-OAEP-SHA256 encryption
- Transaction receipts and confirmations

## 🔮 Roadmap

- [x] Circle API integration
- [x] Arc blockchain transactions
- [x] AI agent reasoning (Semantic Kernel + OpenAI)
- [x] Budget management
- [ ] Circle Gateway (cross-chain balance aggregation)
- [ ] CCTP (Cross-Chain Transfer Protocol for bridging)
- [ ] Gas Station (transaction fee sponsorship)
- [ ] Microsoft Agent Framework migration
- [ ] x402 payment protocol (pay-per-API-call)
- [ ] Agent marketplace
- [ ] Analytics dashboard
- [ ] Multi-wallet support (one wallet per agent)
- [ ] Database persistence (currently in-memory)
- [ ] Authentication & authorization

## 🏗️ Why Circle + Arc?

**Arc is purpose-built for payments and autonomous agents:**

- ✅ **Native USDC** - No gas tokens needed
- ✅ **Sub-second finality** - Instant settlement
- ✅ **Simple transfers** - Wallet-to-wallet, no smart contracts
- ✅ **Built for commerce** - Optimized for payments, not DeFi

**Traditional blockchain pain points:**
- ❌ Managing gas tokens (ETH, MATIC, ARB, etc.)
- ❌ Bridge complexity and fees
- ❌ Slow finality (15+ seconds)
- ❌ Complex DeFi primitives

**Arc eliminates all of this.**

## 💡 Use Cases

- **Autonomous purchasing agents** - AI agents that research and buy services
- **API credit management** - Agents automatically purchase API credits
- **Cross-chain treasury management** - Unified view of USDC across chains
- **Budget-constrained automation** - Agents that operate within spending limits
- **Agent marketplaces** - Agents buying/selling services from each other

## 📝 License

MIT

## 🤝 Contributing

PRs welcome! This is an experimental platform for exploring autonomous agent commerce.

## ⚠️ Important Notes

- This is experimental software
- Use testnet only for development
- Not financial advice
- Autonomous agents can spend real money - use with caution

## 🙏 Acknowledgments

Built with:
- [Circle Developer Controlled Wallets](https://developers.circle.com/w3s)
- [Arc Blockchain](https://developers.circle.com/circle-arc)
- [Microsoft Semantic Kernel](https://github.com/microsoft/semantic-kernel)
- [OpenAI](https://openai.com)

Inspired by Kyle Chassé's vision of autonomous agent orchestration as the key survival skill for 2026.

---

**Built in one day using AI-assisted development.** 🚀
