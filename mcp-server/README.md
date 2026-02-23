# AgentRails MCP Server

A [Model Context Protocol](https://modelcontextprotocol.io) (MCP) server that wraps the AgentRails REST API, giving AI assistants like Claude direct access to agent management, x402 payments, billing, and policy controls.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later

## Quick Start

```bash
# Clone and run
git clone https://github.com/AgenticCommerce/AgenticCommerce.git
cd AgenticCommerce
dotnet run --project mcp-server
```

The server starts in **stdio** mode by default (for Claude Code / Claude Desktop).

For **Streamable HTTP** mode (Copilot Studio, web clients):

```bash
dotnet run --project mcp-server -- --http
```

## Configuration

| Environment Variable | Default | Description |
|---|---|---|
| `AGENTRAILS_BASE_URL` | `https://sandbox.agentrails.io` | AgentRails API base URL |
| `AGENTRAILS_API_KEY` | _(empty)_ | Bearer token for authenticated endpoints |

Public endpoints like `get_x402_pricing` work without an API key. All other tools require authentication.

## Register with Claude Code

The repo includes a `.mcp.json` at the root that auto-registers the server:

```json
{
  "mcpServers": {
    "agentrails": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "mcp-server"],
      "env": {
        "AGENTRAILS_BASE_URL": "https://sandbox.agentrails.io",
        "AGENTRAILS_API_KEY": ""
      }
    }
  }
}
```

## Register with Claude Desktop

Add to your `claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "agentrails": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/AgenticCommerce/mcp-server"]
    }
  }
}
```

## Available Tools

### Agent Management

| Tool | Description |
|---|---|
| `list_agents` | List all agents in the current organization |
| `get_agent` | Get details of a specific agent by ID |
| `create_agent` | Create a new agent (name, description, budget) |
| `delete_agent` | Delete an agent by ID |
| `run_agent` | Execute a task with a specific agent |

### x402 Payments

| Tool | Description |
|---|---|
| `get_x402_pricing` | Get pricing info for x402-protected endpoints (no auth required) |
| `get_x402_payments` | Get payment history with optional filters (network, status, limit) |
| `get_x402_stats` | Get aggregate payment statistics |
| `execute_test_payment` | Execute a test payment on sandbox |

### Billing

| Tool | Description |
|---|---|
| `get_billing_usage` | Get usage summary for the organization (configurable lookback days) |

### Policies

| Tool | Description |
|---|---|
| `list_policies` | List all organization policies |
| `create_policy` | Create a policy (name, approval rules, spending limits) |

## Error Handling

Tools return structured error JSON when API calls fail, so the AI assistant can interpret and act on errors:

```json
{
  "error": true,
  "statusCode": 401,
  "message": "Unauthorized"
}
```

## Development

```bash
# Build
dotnet build mcp-server/

# Run in stdio mode
dotnet run --project mcp-server

# Run in HTTP mode
dotnet run --project mcp-server -- --http
```
