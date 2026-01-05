# Autonomous Agent Commerce Platform

> AI agents that research, decide, and execute USDC payments autonomously on Arc blockchain

Built with Circle Developer Controlled Wallets, Arc blockchain, and OpenAI GPT-4o for fully autonomous commerce.

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Circle](https://img.shields.io/badge/Circle-API-00D395)](https://developers.circle.com/)
[![Arc](https://img.shields.io/badge/Arc-Blockchain-4A90E2)](https://www.circle.com/en/pressroom/circle-announces-arc)
[![OpenAI](https://img.shields.io/badge/OpenAI-GPT--4o-412991?logo=openai)](https://openai.com/)

## 🎯 What This Does

Autonomous AI agents that can:
- 🧠 **Research options** using built-in knowledge bases
- 💡 **Make informed decisions** via GPT-4o reasoning
- 💰 **Execute USDC payments** on Arc blockchain
- 📊 **Manage budgets** with constraint enforcement
- ⚡ **Settle instantly** with sub-second finality

**No human intervention required.**

## ✨ Key Features

### Autonomous Decision-Making
- AI-powered research and analysis
- Budget validation and enforcement
- Risk assessment and strategy planning
- Multi-step reasoning with tool use

### Circle + Arc Integration
- Developer Controlled Wallets for agent custody
- Native USDC settlement on Arc blockchain
- Sub-second transaction finality
- Predictable USDC-based gas fees
- No volatile gas tokens required

### Production-Ready API
- RESTful endpoints with OpenAPI/Swagger
- Agent lifecycle management
- Transaction tracking and receipts
- Budget monitoring
- Comprehensive error handling

## 🚀 Quick Start

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)
- [Circle Developer Account](https://developers.circle.com/)
- [OpenAI API Key](https://platform.openai.com/)

### 1. Clone the Repository
```bash
git clone https://github.com/kmatthewsio/AgenticCommerce.git
cd AgenticCommerce
```

### 2. Configure Secrets

Create `src/AgenticCommerce.API/appsettings.Development.json`:
```json
{
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

### 3. Run the Application
```bash
cd src/AgenticCommerce.API
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
6. Return transaction proof

### Example Response
```json
{
  "success": true,
  "result": "Research complete. Selected Google Gemini 1.5 Flash ($0.075/1M tokens).\n\nPurchase executed:\n- Amount: $1 USDC\n- Transaction ID: 41b0f4e7-299b-5368-93da-15e41892f656\n- Recipient: 0x6255d8dd3f84ec460fc8b07db58ab06384a2f487",
  "amountSpent": 1.0,
  "transactionIds": ["41b0f4e7-299b-5368-93da-15e41892f656"]
}
```

**Transaction proof on Arc testnet:** ✅

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
│                    │ • Arc Client            │  │
│                    │ • Gateway Client        │  │
│                    │ • USDC Transactions     │  │
│                    └────────────────────────┘  │
└───────────────────────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────┐
│         AgenticCommerce.Core                     │
│  (Domain Models + Interfaces)                    │
└──────────────────────────────────────────────────┘
```

## 🛠️ Tech Stack

- **Backend:** ASP.NET Core 8.0 / C#
- **Blockchain:** Circle Developer Controlled Wallets + Arc
- **AI:** OpenAI GPT-4o + Microsoft Semantic Kernel 1.30.0
- **Authentication:** RSA-OAEP-SHA256 encryption
- **API:** REST with OpenAPI/Swagger documentation
- **Storage:** In-memory (database persistence coming soon)

## 🎯 Use Cases

### Corporate Procurement
AI agents that research vendors, compare pricing, and execute purchases within approved budgets.

### Trading Bots
Autonomous agents that analyze markets and execute trades with built-in risk management.

### API Credit Management
Agents that monitor usage, optimize provider selection, and automatically purchase credits.

### Subscription Management
Autonomous renewal and cancellation based on usage patterns and budget constraints.

## 🗺️ Roadmap

### ✅ Phase 1: Core Infrastructure (Complete)
- [x] Circle API integration
- [x] Arc blockchain settlement
- [x] AI agent reasoning (GPT-4o)
- [x] Autonomous execution
- [x] Budget management
- [x] Transaction tracking

### 🚧 Phase 2: Production Features (In Progress)
- [ ] Database persistence (SQLite/PostgreSQL)
- [ ] Circle Gateway (cross-chain balance)
- [ ] x402 payment protocol
- [ ] Enhanced error handling
- [ ] Rate limiting

### 📋 Phase 3: Advanced Features (Planned)
- [ ] Multi-agent orchestration
- [ ] Agent marketplace
- [ ] Analytics dashboard
- [ ] Webhook notifications
- [ ] Custom agent strategies

### 🎨 Phase 4: User Experience (Planned)
- [ ] Web dashboard (React/Next.js)
- [ ] Mobile app
- [ ] Agent templates
- [ ] Visual workflow builder

## 🔐 Security

- API keys encrypted at rest
- RSA-OAEP-SHA256 for Circle authentication
- Budget constraints enforced cryptographically
- Transaction validation before execution
- Comprehensive audit logging

## 📊 Circle Gateway Status

Circle Gateway integration is implemented and ready for cross-chain USDC balance aggregation. Current testnet support includes:
- Ethereum (Sepolia)
- Avalanche (Fuji)
- Base (Sepolia)
- Arc

Additional chains will activate as Circle enables testnet support.

## 🤝 Contributing

This is a personal project demonstrating autonomous agent commerce. Feedback and suggestions welcome!

## 📄 License

MIT License - See [LICENSE](LICENSE) file for details

## 🙏 Acknowledgments

Built with:
- [Circle](https://www.circle.com/) - USDC infrastructure and Arc blockchain
- [OpenAI](https://openai.com/) - GPT-4o for agent reasoning
- [Microsoft Semantic Kernel](https://github.com/microsoft/semantic-kernel) - AI orchestration

Special thanks to the Circle Developer Relations team for Arc and Gateway documentation.

## 📞 Contact

Questions? Reach out:
- GitHub Issues: [Create an issue](https://github.com/yourusername/AgenticCommerce/issues)
- Twitter: https://x.com/kevlondonbtc

**Built in one day using AI-assisted development.** 🚀
