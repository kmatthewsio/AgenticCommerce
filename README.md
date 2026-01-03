# **Agentic Commerce Platform**

# 

# Autonomous AI agents that can manage budgets and execute blockchain transactions.

# 

#  What It Does

# 

# \- \*\*Create AI Agents\*\* with spending budgets

# \- \*\*Autonomous Purchases\*\* on Arc blockchain via Circle API

# \- \*\*Budget Management\*\* - agents track spending limits

# \- \*\*Real-time Transactions\*\* - sub-second settlement on Arc

# \- \*\*Full REST API\*\* - Swagger documentation included

# \*\*First autonomous agent purchase:\*\*

# \- Transaction: `b8f15a06-fe88-501b-9d23-4c9ae57fe130`

# \- Amount: 1 USDC

# \- Network: Arc Testnet

# \- Status: ✅ Complete

# 

**# Tech Stack**

# 

# \- \*\*Backend:\*\* ASP.NET Core 8.0

# \- \*\*Blockchain:\*\* Circle Developer Controlled Wallets + Arc

# \- \*\*Authentication:\*\* RSA-OAEP-SHA256 encryption

# \- \*\*API:\*\* REST with Swagger/OpenAPI

# 

**# Architecture**

# ```

# API Layer (Controllers)

# &nbsp;   ↓

# Agent Service (Budget Management)

# &nbsp;   ↓

# Arc Client (Circle API Integration)

# &nbsp;   ↓

# Circle Developer Controlled Wallets

# &nbsp;   ↓

# Arc Blockchain (USDC Settlement)

# ```

# 

**# Quick Start**

# 

# \### Prerequisites

# \- .NET 8.0 SDK

# \- Circle Developer Account

# \- Arc Testnet USDC

# 

**# Setup**

# 

# 1\. Clone the repo

# ```bash

# git clone https://github.com/YOUR\_USERNAME/agentic-commerce.git

# cd agentic-commerce

# ```

# 

# 2\. Configure Circle credentials in `src/AgenticCommerce.API/Properties/launchSettings.json`:

# ```json

# {

# &nbsp; "Circle\_\_ApiKey": "YOUR\_API\_KEY",

# &nbsp; "Circle\_\_EntitySecret": "YOUR\_ENTITY\_SECRET",

# &nbsp; "Circle\_\_WalletAddress": "YOUR\_WALLET\_ADDRESS",

# &nbsp; "Circle\_\_WalletId": "YOUR\_WALLET\_ID"

# }

# ```

# 

# 3\. Run

# ```bash

# cd src/AgenticCommerce.API

# dotnet run

# ```

# 

# 4\. Open Swagger UI: `https://localhost:7098/swagger`

# 

**# Usage**

# 

# Create an Agent

# ```bash

# POST /api/agents

# {

# &nbsp; "name": "Shopping Agent",

# &nbsp; "budget": 100.00,

# &nbsp; "capabilities": \["purchase", "research"]

# }

# ```

# 

**# Make a Purchase**

# ```bash

# POST /api/agents/{agentId}/purchase

# {

# &nbsp; "recipientAddress": "0x...",

# &nbsp; "amount": 1.0,

# &nbsp; "description": "Autonomous purchase"

# }

# ```

# 

**# Check Agent Status**

# ```bash

# GET /api/agents/{agentId}/info

# ```

# 

**# API Endpoints**

# 

# \- `POST /api/agents` - Create agent

# \- `GET /api/agents` - List agents

# \- `POST /api/agents/{id}/purchase` - Execute purchase

# \- `POST /api/agents/{id}/run` - Run agent task

# \- `GET /api/transactions/balance` - Check wallet balance

# \- `GET /api/transactions/{id}` - Get transaction details

# 

**# Roadmap**

# 

# \- \[ ] Microsoft Agent Framework integration (real AI reasoning)

# \- \[ ] Circle Gateway (cross-chain balance aggregation)

# \- \[ ] x402 payment protocol (pay-per-API-call)

# \- \[ ] Agent marketplace

# \- \[ ] Analytics dashboard

# \- \[ ] Multi-wallet support (one wallet per agent)

