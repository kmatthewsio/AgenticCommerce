# AgentRails User Guide

This guide covers how to use the AgentRails admin dashboard and REST APIs for managing autonomous AI agents, policies, and transactions.

---

## Table of Contents

1. [Getting Started](#getting-started)
2. [Admin Dashboard](#admin-dashboard)
3. [API Reference](#api-reference)
4. [Authentication](#authentication)
5. [Examples](#examples)

---

## Getting Started

### Prerequisites

- AgentRails API running (default: `https://localhost:7098`)
- A registered user account with an organization

### Quick Start

1. Navigate to `https://localhost:7098/admin/index.html`
2. Register a new account or log in with existing credentials
3. Start managing your agents, policies, and transactions

---

## Admin Dashboard

The admin dashboard provides a visual interface for managing all aspects of the AgentRails platform.

### Login / Registration

#### Register a New Account
1. Click "Don't have an account? Register"
2. Fill in:
   - **Name**: Your display name
   - **Email**: Your email address
   - **Password**: A secure password
   - **Organization Name**: Your company or organization name
3. Click "Register"

#### Login
1. Enter your email and password
2. Click "Login"

---

### Navigation

The sidebar provides access to all dashboard sections:

| Section | Description |
|---------|-------------|
| **Overview** | Dashboard summary with stats and trends |
| **Agents** | Manage autonomous AI agents |
| **Transactions** | View transaction history |
| **Policies** | Configure spending policies |
| **API Keys** | Manage API authentication keys |
| **Logs** | View system logs and errors |

---

### Overview Page

The Overview page displays:

#### Stats Cards
- **Total Agents**: Number of agents in your organization
- **Active Agents**: Agents currently operational
- **Total Budget**: Combined budget across all agents
- **Total Spent**: Total amount spent by agents

Each card includes:
- A trend indicator showing change over the past week
- A sparkline chart showing recent activity

#### Quick Actions
- Create new agents
- View recent transactions
- Monitor system health

---

### Agents Page

#### Viewing Agents

The agents table displays:
- **Name**: Agent identifier
- **Status**: Active, Inactive, or Paused
- **Budget**: Total allocated budget
- **Balance**: Current remaining balance
- **Created**: Creation timestamp

#### Table Features

**Sorting**: Click any column header to sort
- Click once for ascending order
- Click again for descending order
- Arrow indicator shows current sort direction

**Filtering**: Use the search box to filter agents by:
- Name
- ID
- Status

#### Creating an Agent

1. Click "Create Agent"
2. Fill in the form:
   - **Name** (required): A descriptive name for the agent
   - **Description**: What the agent does
   - **Budget** (required): Maximum spending limit in USDC
3. Click "Create"

**Validation**:
- Name is required
- Budget must be greater than 0

#### Deleting an Agent

1. Click the trash icon on the agent row
2. Review the confirmation modal
3. Click "Delete" to confirm or "Cancel" to abort

---

### Transactions Page

#### Viewing Transactions

The transactions table displays:
- **ID**: Unique transaction identifier
- **Agent**: The agent that initiated the transaction
- **Amount**: Transaction amount in USDC
- **Recipient**: Destination wallet address
- **Status**: Completed, Pending, or Failed
- **Date**: Transaction timestamp

#### Transaction Statuses

| Status | Description |
|--------|-------------|
| **Completed** | Successfully settled on blockchain |
| **Pending** | Awaiting confirmation |
| **Failed** | Transaction rejected or timed out |

#### Filtering Transactions

Use the search box to filter by:
- Transaction ID
- Agent ID
- Recipient address
- Description

---

### Policies Page

Policies define spending rules and restrictions for your agents.

#### Viewing Policies

The policies table displays:
- **Name**: Policy identifier
- **Description**: Policy purpose
- **Rules**: Number of active rules
- **Status**: Enabled or Disabled
- **Created**: Creation timestamp

#### Creating a Policy

1. Click "Create Policy"
2. Fill in the form:
   - **Name** (required): Policy name
   - **Description**: What the policy enforces
   - **Max Transaction Amount**: Maximum single transaction (USDC)
   - **Daily Spending Limit**: Maximum daily spending (USDC)
   - **Requires Approval**: Toggle for manual approval requirement
3. Click "Create"

#### Policy Rules

| Rule Type | Description |
|-----------|-------------|
| **MaxPerTransaction** | Limits individual transaction amounts |
| **DailyLimit** | Caps total daily spending |
| **AllowedNetworks** | Restricts to specific blockchains |
| **MaxTransactionsPerHour** | Rate limits transaction frequency |
| **RequiresApproval** | Requires manual approval |

#### Deleting a Policy

1. Click the trash icon on the policy row
2. Confirm in the modal dialog
3. Policy is removed (existing transactions unaffected)

---

### API Keys Page

API keys allow programmatic access to your organization's resources.

#### Viewing API Keys

The table displays:
- **Name**: Key identifier
- **Key Prefix**: First characters for identification
- **Created**: Creation timestamp
- **Last Used**: Most recent usage

#### Creating an API Key

1. Click "Create API Key"
2. Enter a **Name** for the key (e.g., "Production", "Development")
3. Click "Create"
4. **Important**: Copy the displayed key immediately - it won't be shown again

#### Using API Keys

Include the key in the `X-API-Key` header:

```
X-API-Key: your-api-key-here
```

#### Revoking an API Key

1. Click the trash icon on the key row
2. Confirm revocation
3. The key is immediately invalidated

---

### Logs Page

View system logs for debugging and monitoring.

#### Log Levels

| Level | Color | Description |
|-------|-------|-------------|
| **Error** | Red | Critical failures requiring attention |
| **Warning** | Yellow | Potential issues to monitor |
| **Information** | Blue | Normal operational events |

#### Filtering Logs

1. **Search**: Filter by message content or source
2. **Level Filter**: Use the dropdown to show only specific levels:
   - All Levels
   - Error
   - Warning
   - Information

#### Log Details

Each log entry shows:
- **Timestamp**: When the event occurred
- **Level**: Severity indicator
- **Message**: Event description
- **Source**: Component that generated the log

---

## API Reference

Base URL: `https://localhost:7098/api`

### Authentication Endpoints

#### Register
```http
POST /api/auth/register
Content-Type: application/json

{
  "name": "John Doe",
  "email": "john@example.com",
  "password": "SecurePass123",
  "organizationName": "Acme Corp"
}
```

**Response:**
```json
{
  "accessToken": "eyJhbGc...",
  "refreshToken": "abc123...",
  "user": {
    "id": "user-id",
    "email": "john@example.com",
    "name": "John Doe",
    "organizationId": "org-id",
    "organizationName": "Acme Corp"
  }
}
```

#### Login
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "john@example.com",
  "password": "SecurePass123"
}
```

#### Refresh Token
```http
POST /api/auth/refresh
Content-Type: application/json

{
  "refreshToken": "your-refresh-token"
}
```

---

### Agents Endpoints

#### List Agents
```http
GET /api/agents
Authorization: Bearer {token}
```

#### Get Agent Dashboard Data
```http
GET /api/agents/dashboard
Authorization: Bearer {token}
```

**Response:**
```json
{
  "agents": [...],
  "summary": {
    "totalAgents": 5,
    "activeAgents": 4,
    "totalBudget": 1000.00,
    "totalSpent": 150.00
  }
}
```

#### Create Agent
```http
POST /api/agents
Authorization: Bearer {token}
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
Authorization: Bearer {token}
```

#### Delete Agent
```http
DELETE /api/agents/{agentId}
Authorization: Bearer {token}
```

#### Make Purchase (Agent Action)
```http
POST /api/agents/{agentId}/purchase
Authorization: Bearer {token}
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
Authorization: Bearer {token}
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

### Policies Endpoints

#### List Policies
```http
GET /api/policies
Authorization: Bearer {token}
```

#### Create Policy
```http
POST /api/policies
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "Conservative Policy",
  "description": "Low-risk spending limits",
  "maxTransactionAmount": 50.00,
  "dailySpendingLimit": 200.00,
  "requiresApproval": false,
  "enabled": true
}
```

#### Get Policy Details
```http
GET /api/policies/{policyId}
Authorization: Bearer {token}
```

#### Update Policy
```http
PUT /api/policies/{policyId}
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "Updated Policy",
  "enabled": false
}
```

#### Delete Policy
```http
DELETE /api/policies/{policyId}
Authorization: Bearer {token}
```

---

### API Keys Endpoints

#### List API Keys
```http
GET /api/auth/api-keys
Authorization: Bearer {token}
```

#### Create API Key
```http
POST /api/auth/api-keys
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "Production Key"
}
```

**Response:**
```json
{
  "id": "key-id",
  "name": "Production Key",
  "key": "ar_live_abc123...",  // Only shown once!
  "createdAt": "2024-01-22T12:00:00Z"
}
```

#### Revoke API Key
```http
DELETE /api/auth/api-keys/{keyId}
Authorization: Bearer {token}
```

---

### Logs Endpoints

#### Get Logs
```http
GET /api/logs?count=100&level=Error
```

**Query Parameters:**
- `count` (optional): Number of logs (default: 100)
- `level` (optional): Filter by level (Error, Warning, Information)

#### Get Errors Only
```http
GET /api/logs/errors?count=50
```

#### Get Warnings Only
```http
GET /api/logs/warnings?count=50
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

**Response:**
```json
{
  "service": "Agentic Commerce Backend",
  "version": "v1.1.0",
  "status": "Running",
  "edition": "Enterprise",
  "features": [...]
}
```

---

## Authentication

AgentRails supports two authentication methods:

### JWT Bearer Token

1. Obtain a token via `/api/auth/login` or `/api/auth/register`
2. Include in requests:
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

### API Key

1. Create an API key via the dashboard or API
2. Include in requests:
```
X-API-Key: ar_live_abc123...
```

### Token Refresh

Access tokens expire after a configured period. Use the refresh token to obtain a new access token:

```http
POST /api/auth/refresh
Content-Type: application/json

{
  "refreshToken": "your-refresh-token"
}
```

---

## Examples

### Create an Agent and Make a Purchase

```bash
# 1. Login
TOKEN=$(curl -s -X POST https://localhost:7098/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"password"}' \
  | jq -r '.accessToken')

# 2. Create an agent
AGENT=$(curl -s -X POST https://localhost:7098/api/agents \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name":"My Agent","description":"Test agent","budget":100}')

AGENT_ID=$(echo $AGENT | jq -r '.id')

# 3. Make a purchase
curl -X POST "https://localhost:7098/api/agents/$AGENT_ID/purchase" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"amount":10,"recipientAddress":"0x123...","description":"Test purchase"}'
```

### Create a Policy with Rules

```bash
# Create a conservative policy
curl -X POST https://localhost:7098/api/policies \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Conservative Policy",
    "description": "Strict spending limits",
    "maxTransactionAmount": 25.00,
    "dailySpendingLimit": 100.00,
    "requiresApproval": false,
    "enabled": true
  }'
```

### Using API Key Authentication

```bash
# Create an API key (save the returned key!)
API_KEY=$(curl -s -X POST https://localhost:7098/api/auth/api-keys \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name":"CLI Key"}' \
  | jq -r '.key')

# Use API key for subsequent requests
curl https://localhost:7098/api/agents \
  -H "X-API-Key: $API_KEY"
```

---

## Troubleshooting

### Common Issues

| Issue | Solution |
|-------|----------|
| 401 Unauthorized | Check token expiration, refresh or re-login |
| 403 Forbidden | Verify organization permissions |
| 404 Not Found | Check endpoint URL and resource IDs |
| 500 Server Error | Check logs for details |

### Dashboard Not Loading

1. Ensure the API is running (`curl https://localhost:7098/health`)
2. Clear browser cache
3. Check browser console for errors

### Transactions Failing

1. Verify wallet has sufficient balance
2. Check policy restrictions
3. Review agent budget limits

---

## Support

- **API Documentation**: `/swagger` (Swagger UI)
- **Health Check**: `/health`
- **Logs**: Dashboard Logs page or `/api/logs`

For additional help, check the system logs or contact your administrator.
