# CLAUDE.md - Project Context for Claude Code

## Project Overview

AgenticCommerce is an autonomous AI agent commerce platform that enables:
- AI agents to research, decide, and execute USDC payments on Arc blockchain
- API monetization via the x402 payment protocol (HTTP 402)
- Circle Developer Controlled Wallets for agent custody

## Tech Stack

- **Backend:** ASP.NET Core 8.0 / C#
- **Database:** PostgreSQL 16 + Entity Framework Core 8
- **Blockchain:** Circle Developer Controlled Wallets + Arc
- **AI:** OpenAI GPT-4o + Microsoft Semantic Kernel
- **Payments:** x402 V2 Protocol (EIP-3009)

## Project Structure

```
src/
├── AgenticCommerce.API/           # REST API layer
│   ├── Controllers/
│   │   ├── X402Controller.cs      # Main x402 endpoints (630 lines)
│   │   ├── X402ExampleController.cs # Attribute-based examples
│   │   ├── AgentsController.cs    # Agent management
│   │   └── TransactionsController.cs
│   └── Program.cs                 # DI configuration
│
├── AgenticCommerce.Infrastructure/ # Implementation layer
│   ├── Payments/
│   │   ├── X402Service.cs         # Core x402 logic (338 lines)
│   │   ├── X402PaymentService.cs  # Legacy in-memory service
│   │   ├── X402PaymentFilter.cs   # Action filter middleware (199 lines)
│   │   ├── X402PaymentAttribute.cs # [X402Payment] decorator
│   │   ├── X402Client.cs          # Agent HTTP client (248 lines)
│   │   └── X402Extensions.cs      # Helper methods
│   ├── Blockchain/
│   │   ├── ArcClient.cs           # Circle Arc integration (459 lines)
│   │   └── CircleGatewayClient.cs
│   ├── Data/
│   │   ├── AgenticCommerceDbContext.cs
│   │   └── Migrations/
│   └── Agents/
│       └── AgentService.cs
│
└── AgenticCommerce.Core/          # Domain models & interfaces
    ├── Models/
    │   ├── X402Spec.cs            # V2 spec models (305 lines)
    │   ├── X402PaymentEntity.cs   # Database entity
    │   └── Agent.cs
    └── Interfaces/
        ├── IX402Service.cs
        └── IArcClient.cs
```

## x402 Payment Protocol Implementation

### Key Files

| File | Purpose |
|------|---------|
| `X402Spec.cs` | V2 specification models, network constants, header names |
| `X402Service.cs` | Payment verification & settlement logic |
| `X402PaymentFilter.cs` | Middleware that intercepts requests and enforces payment |
| `X402PaymentAttribute.cs` | `[X402Payment(0.01)]` decorator for endpoints |
| `X402Client.cs` | HTTP client that auto-handles 402 responses |
| `X402Controller.cs` | REST endpoints for payments, facilitator, analytics |
| `Eip3009SignatureVerifier.cs` | EIP-712 typed data hashing & ECDSA signature recovery |

### x402 Flow

1. Client requests protected resource
2. Server returns 402 with `X-PAYMENT-REQUIRED` header (base64-encoded requirements)
3. Client signs EIP-3009 authorization
4. Client retries with `X-PAYMENT` header containing signed payload
5. Server verifies signature, settles on-chain, returns `X-PAYMENT-RESPONSE`

### Adding x402 to an Endpoint

```csharp
[X402Payment(0.01, Description = "Premium API call")]
[HttpGet("premium")]
public IActionResult Premium()
{
    var payer = HttpContext.GetX402Payer();
    var txHash = HttpContext.GetX402TransactionHash();
    return Ok(new { data = "Premium content", payer, txHash });
}
```

### Multi-Network Support

```csharp
[X402Payment(0.01,
    Network = "arc-testnet",
    AllowMultipleNetworks = true,
    AlternativeNetworks = "base-sepolia,ethereum-sepolia")]
```

## Arc Integration (Completed)

### EIP-3009 Signature Verification
- `Eip3009SignatureVerifier.cs` - Off-chain verification using EIP-712 typed data hashing
- Uses Nethereum.Signer for ECDSA signature recovery

### EIP-3009 Signing
- `Eip3009Signer.cs` - Signs TransferWithAuthorization using Circle's Developer Controlled Wallets API
- Builds EIP-712 typed data and calls `/developer/sign/typedData` endpoint

### Arc Network Configuration
- Chain ID: 5042002 (testnet)
- USDC Contract: `0x3600000000000000000000000000000000000000` (native gas token with ERC-20 interface)
- Mainnet: Not yet launched (expected 2026)

## Commands

```bash
# Run the API
cd src/AgenticCommerce.API
dotnet run --launch-profile https

# Run tests
cd tests/AgenticCommerce.Tests
dotnet test

# Add a migration
cd src/AgenticCommerce.Infrastructure
dotnet ef migrations add MigrationName --startup-project ../AgenticCommerce.API

# Apply migrations (happens automatically on startup)
```

## Configuration

Secrets are stored via .NET User Secrets or environment variables:

```bash
# Circle Configuration
Circle:ApiKey
Circle:EntitySecret
Circle:WalletAddress
Circle:WalletId

# OpenAI Configuration
OpenAI:ApiKey
OpenAI:Model

# Database
ConnectionStrings:DefaultConnection
```

## Database

PostgreSQL with auto-migration on startup. Key tables:
- `agents` - AI agent definitions and state
- `x402_payments` - Payment audit trail with indexes on payment_id, payer_address, transaction_hash, status, network

## API Endpoints

### x402 Payment Endpoints
- `GET /api/x402/protected/analysis` - $0.01 USDC
- `GET /api/x402/protected/data` - $0.001 USDC
- `GET /api/x402/pricing` - Pricing info

### Facilitator Endpoints
- `POST /api/x402/facilitator/verify` - Verify payment payloads
- `POST /api/x402/facilitator/settle` - Execute settlement

### Analytics
- `GET /api/x402/payments` - Payment history
- `GET /api/x402/stats` - Statistics

### Test Endpoints (Development only)
- `GET /api/x402/test/wallet-status`
- `GET /api/x402/test/generate-payload`
- `POST /api/x402/test/execute-payment`

## Networks Supported

| Network | Chain ID | Environment |
|---------|----------|-------------|
| arc-testnet | 5042002 | Development |
| arc-mainnet | TBD | Production |
| base-sepolia | 84532 | Development |
| base-mainnet | 8453 | Production |
| ethereum-sepolia | 11155111 | Development |
| ethereum-mainnet | 1 | Production |

## Key Constants

```csharp
// Headers
X402Headers.PaymentRequired = "X-PAYMENT-REQUIRED"
X402Headers.Payment = "X-PAYMENT"
X402Headers.PaymentResponse = "X-PAYMENT-RESPONSE"

// USDC has 6 decimals
$0.01 = 10000 smallest units
$1.00 = 1000000 smallest units
```
