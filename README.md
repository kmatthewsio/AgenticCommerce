# AgentRails

> The orchestration layer between AI agents and the financial system

**Agent-agnostic. Model-agnostic. Enterprise-ready.**

AgentRails connects any AI agent to payment infrastructure with the budget controls, transaction logging, and compliance that businesses demand.

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Circle](https://img.shields.io/badge/Circle-API-00D395)](https://developers.circle.com/)
[![Arc](https://img.shields.io/badge/Arc-Blockchain-4A90E2)](https://www.circle.com/en/pressroom/circle-announces-arc)
[![x402](https://img.shields.io/badge/x402-V2%20Spec-FF6B35)](https://www.x402.org/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791?logo=postgresql)](https://www.postgresql.org/)

## The Problem

As AI agents proliferate, they need to transact. But:

- **LLM providers** (OpenAI, Anthropic, Google) build models, not payment rails
- **Agent frameworks** (LangChain, CrewAI, Semantic Kernel) orchestrate reasoning, not money
- **Model routers** (ClawRouter, etc.) optimize inference costs, not compliance

**No one owns the bottleneck between intelligent agents and the financial system.**

## The Solution

AgentRails is payment orchestration infrastructure for the agentic economy:

| What We Do | What We Don't Do |
|------------|------------------|
| Connect agents to financial rails | Build AI models |
| Enforce budget policies | Route to different LLMs |
| Log every transaction | Compete with agent frameworks |
| Provide audit trails | Lock you into our stack |

> *"The value is not in the application. It is in the connective tissue between applications, data, and decisions."*
>
> *— Jordi Visser, "Palantir as Signal"*

AgentRails is the connective tissue between AI agents and the financial system.

## Why This Matters

### The Shift: SaaS to Value-Based Pricing

| Old Model (SaaS) | New Model (Agentic) |
|------------------|---------------------|
| Per-seat pricing | Per-transaction pricing |
| Humans use software | Agents use APIs |
| Monthly subscriptions | Micropayments per action |
| $50/seat/month | $0.001 per API call |

AgentRails provides the payment rails for this new model via the [x402 protocol](https://www.x402.org/).

### The Moat: Switching Costs

Once agents are wired to AgentRails:
- Transaction history lives here
- Budget policies are configured here
- Compliance audit trails are here
- Switching means rebuilding all of it

## Core Capabilities

### Agent-Agnostic Integration
Works with any AI agent, any framework, any model:
- OpenAI GPT-4o, Claude, Gemini, Llama, Mistral
- LangChain, CrewAI, AutoGen, Semantic Kernel
- Custom agents, enterprise agents, open-source agents

### Autonomous Commerce
- **Research options** using built-in knowledge bases
- **Make informed decisions** via AI reasoning
- **Execute USDC payments** on Arc blockchain
- **Manage budgets** with constraint enforcement
- **Settle instantly** with sub-second finality
- **Persist forever** with PostgreSQL database

### x402 Payment Protocol (V2 Spec-Compliant)
- **Pay for APIs** using HTTP 402 micropayments
- **Monetize your APIs** with one line of code
- **EIP-3009** signed authorizations (gasless)
- **Multi-network** support (Arc, Base, Ethereum)

### Enterprise Governance
- Budget controls (per-transaction, daily, weekly limits)
- Destination whitelists/blacklists
- Approval workflows for high-value transactions
- Complete audit trail for compliance

## How It Works

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│   Any AI Agent  │     │   AgentRails    │     │  Financial      │
│                 │     │                 │     │  System         │
│  - GPT-4o       │────▶│  - Orchestrate  │────▶│                 │
│  - Claude       │     │  - Authorize    │     │  - USDC         │
│  - Llama        │     │  - Log          │     │  - Arc/Base     │
│  - Custom       │     │  - Settle       │     │  - Banks (soon) │
└─────────────────┘     └─────────────────┘     └─────────────────┘
                               │
                               ▼
                        ┌─────────────────┐
                        │   Governance    │
                        │                 │
                        │  - Budgets      │
                        │  - Policies     │
                        │  - Audit Logs   │
                        └─────────────────┘
```

### x402 Payment Flow

```
┌─────────────┐                              ┌─────────────┐
│   AI Agent  │                              │  API Server │
└──────┬──────┘                              └──────┬──────┘
       │                                            │
       │  1. GET /api/protected/data                │
       │ ─────────────────────────────────────────► │
       │                                            │
       │  2. 402 Payment Required                   │
       │     X-PAYMENT-REQUIRED: {amount, payTo}    │
       │ ◄───────────────────────────────────────── │
       │                                            │
       │  3. AgentRails signs EIP-3009 auth         │
       │     (budget check + policy validation)     │
       │                                            │
       │  4. GET /api/protected/data                │
       │     X-PAYMENT: {signed_authorization}      │
       │ ─────────────────────────────────────────► │
       │                                            │
       │  5. Verify → Settle → Log                  │
       │                                            │
       │  6. 200 OK + X-PAYMENT-RESPONSE            │
       │     {data + transactionHash}               │
       │ ◄───────────────────────────────────────── │
```

## Quick Start

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (for PostgreSQL)
- [Circle Developer Account](https://developers.circle.com/)

### 1. Clone and Start Database

```bash
git clone https://github.com/agentrails/agentrails.git
cd agentrails

docker run --name agentrails-db \
  -e POSTGRES_PASSWORD=dev_password_change_in_prod \
  -e POSTGRES_DB=agentrails \
  -p 5432:5432 \
  -d postgres:16
```

### 2. Configure Secrets

```bash
cd src/AgenticCommerce.API

# Circle Configuration (for blockchain settlement)
dotnet user-secrets set "Circle:ApiKey" "your-circle-api-key"
dotnet user-secrets set "Circle:EntitySecret" "your-entity-secret-hex"
dotnet user-secrets set "Circle:WalletAddress" "0xYourWalletAddress"
dotnet user-secrets set "Circle:WalletId" "your-wallet-id"

# Optional: OpenAI for built-in agent reasoning
dotnet user-secrets set "OpenAI:ApiKey" "your-openai-api-key"
```

### 3. Run

```bash
dotnet run --launch-profile https
```

Navigate to `https://localhost:7098/swagger` to explore the API.

## Monetize Your API in One Line

```csharp
[X402Payment(0.01, Description = "Premium analysis")]
[HttpGet("analysis")]
public IActionResult Analysis()
{
    var payer = HttpContext.GetX402Payer();
    return Ok(new {
        data = "Premium content",
        paidBy = payer
    });
}
```

That's it. Your endpoint now requires $0.01 USDC per request. AgentRails handles verification, settlement, and logging automatically.

## API Endpoints

### Agent Management
| Endpoint | Description |
|----------|-------------|
| `POST /api/agents` | Create agent with budget |
| `POST /api/agents/{id}/run` | Execute autonomous task |
| `POST /api/agents/{id}/purchase` | Direct payment execution |
| `GET /api/agents/{id}` | Get agent state |

### x402 Payments
| Endpoint | Description |
|----------|-------------|
| `GET /api/x402/protected/analysis` | Example paid endpoint ($0.01) |
| `GET /api/x402/protected/data` | Micropayment endpoint ($0.001) |
| `POST /api/x402/facilitator/verify` | Verify payment signatures |
| `POST /api/x402/facilitator/settle` | Execute on-chain settlement |
| `GET /api/x402/payments` | Payment history |
| `GET /api/x402/stats` | Analytics |

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    AgentRails.API                           │
│              (REST Controllers + x402 Middleware)           │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │           ORCHESTRATION LAYER                       │   │
│  ├─────────────────────────────────────────────────────┤   │
│  │  • Agent lifecycle management                       │   │
│  │  • Budget enforcement                               │   │
│  │  • Policy evaluation                                │   │
│  │  • x402 payment handling                            │   │
│  │  • Transaction logging                              │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  ┌──────────────────┐    ┌────────────────────────────┐   │
│  │  Blockchain      │    │     Persistence            │   │
│  │  • Circle DCW    │    │     • Agents               │   │
│  │  • Arc Network   │    │     • Transactions         │   │
│  │  • EVM Chains    │    │     • x402 Payments        │   │
│  │  • EIP-3009      │    │     • Audit Logs           │   │
│  └──────────────────┘    └────────────────────────────┘   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## Tech Stack

- **Backend:** ASP.NET Core 8.0 / C#
- **Database:** PostgreSQL 16 + Entity Framework Core 8
- **Blockchain:** Circle Developer Controlled Wallets + Arc
- **Payments:** x402 V2 Protocol (EIP-3009/EIP-712)
- **Crypto:** Nethereum.Signer for ECDSA signatures
- **API:** REST with OpenAPI/Swagger

## Network Support

| Network | Chain ID | Environment |
|---------|----------|-------------|
| arc-testnet | 5042002 | Development |
| arc-mainnet | TBD | Production |
| base-sepolia | 84532 | Development |
| base-mainnet | 8453 | Production |
| ethereum-sepolia | 11155111 | Development |
| ethereum-mainnet | 1 | Production |

## Positioning

### What AgentRails Is

**The orchestration layer for agent payments** — like Palantir connects AI to enterprise data, AgentRails connects AI agents to financial rails.

| Layer | Who Wins | Example |
|-------|----------|---------|
| AI Models | Commoditizing | OpenAI, Anthropic, open source |
| Agent Frameworks | Fragmented | LangChain, CrewAI, Semantic Kernel |
| Model Routers | Niche | ClawRouter |
| **Payment Orchestration** | **Winner-take-most** | **AgentRails** |

### What AgentRails Is NOT

- Not an LLM (we're model-agnostic)
- Not an agent framework (we're agent-agnostic)
- Not a model router (we don't optimize inference costs)
- Not a wallet provider (we orchestrate wallets)

> *For LLM cost optimization, we recommend solutions like [ClawRouter](https://github.com/BlockRunAI/ClawRouter). AgentRails focuses on what happens after your agent decides to act—executing compliant, auditable transactions.*

## Enterprise Edition

For regulated industries requiring advanced governance:

- **Policy Engine:** Spend limits, rate limiting, destination controls
- **Approval Workflows:** Human sign-off for high-value transactions
- **Advanced Audit:** Immutable decision logs for compliance
- **Multi-Tenant:** Organization-level isolation
- **Admin Dashboard:** Real-time monitoring

**Contact [sales@agentrails.io](mailto:sales@agentrails.io) for licensing.**

## Security

- API keys encrypted at rest
- RSA-OAEP-SHA256 for Circle authentication
- Budget constraints enforced cryptographically
- EIP-712 typed data signing (tamper-proof)
- EIP-3009 signature verification (ECDSA recovery)
- Complete audit trail for every transaction

## Performance

- **Transaction Finality:** <1 second (Arc deterministic finality)
- **x402 Verification:** <50ms
- **Database Queries:** <50ms (indexed PostgreSQL)

## Roadmap

- [x] Core orchestration infrastructure
- [x] x402 V2 protocol implementation
- [x] Circle/Arc blockchain integration
- [x] Multi-network support
- [x] Enterprise policy engine
- [x] Multi-tenant architecture
- [ ] Fiat on/off ramps
- [ ] Additional blockchain networks
- [ ] SDK for popular languages

## Contributing

We welcome contributions! See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## License

MIT License - See [LICENSE](LICENSE.md) for details.

## Acknowledgments

Built with:
- [Circle](https://www.circle.com/) - USDC infrastructure and Arc blockchain
- [x402.org](https://www.x402.org/) - Payment protocol specification
- [PostgreSQL](https://www.postgresql.org/) - Production-grade database
- [Nethereum](https://nethereum.com/) - .NET Ethereum library

## Contact

- GitHub Issues: [Create an issue](https://github.com/agentrails/agentrails/issues)
- Sales: [sales@agentrails.io](mailto:sales@agentrails.io)
- Twitter: [@agentrails](https://x.com/agentrails)

---

**AgentRails: The connective tissue between AI agents and the financial system.**

*Built for the agentic economy.*
