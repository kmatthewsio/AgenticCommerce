# AgenticCommerce Copilot Studio Plugins

Two OpenAPI (Swagger 2.0) specs for importing into **Microsoft Copilot Studio** as custom connector plugins. Each spec exposes a curated subset of the AgenticCommerce Enterprise API at `api.agentrails.io`.

## Specs

| File | Purpose | Operations |
|------|---------|------------|
| `financeops-copilot.swagger.json` | Revenue, payments, wallet balances, billing | 7 |
| `agent-executor-copilot.swagger.json` | Create, run, manage agents + view logs | 7 |

## Import into Copilot Studio

### 1. Create a new copilot (or open an existing one)

In [Copilot Studio](https://copilotstudio.microsoft.com), create or open a copilot.

### 2. Add a plugin action

1. Go to **Actions** in the left sidebar
2. Click **+ Add an action**
3. Select **OpenAPI** as the action type

### 3. Upload the spec

1. Choose **Upload a file** and select one of the `.swagger.json` files
2. Copilot Studio will parse the operations and show them in a list
3. Review the operations — all 7 should appear with their descriptions

### 4. Configure authentication

1. In the connection setup, select **API Key** as the authentication type
2. Set the header name to `X-API-Key`
3. Enter your AgenticCommerce API key as the value
4. Save the connection

### 5. Test the plugin

Use the test pane in Copilot Studio to try natural language queries.

## Example Prompts

### FinanceOps Copilot

- "What's our total x402 revenue?"
- "Show me recent payments"
- "Are there any failed payments?"
- "What's our wallet balance?"
- "How much USDC is on Base vs Ethereum?"
- "What are the API endpoint prices?"
- "Show me billing usage for this period"

### Agent Executor Copilot

- "List all my agents"
- "Create a new research agent with a $5 budget"
- "Show me details for the analyst agent"
- "Run the research agent with task: summarize today's crypto news"
- "Delete the test agent"
- "Show me recent logs"
- "Are there any errors in the logs?"

## Technical Notes

- **OpenAPI v2 (Swagger 2.0) JSON** — required by Copilot Studio (v3 and YAML are not supported)
- **Flat schemas only** — no nested objects, no `oneOf`/`allOf`/`anyOf`, no circular `$ref`
- **Rich descriptions** — every operation, parameter, and response property has a description that helps the Copilot LLM understand when and how to call each endpoint
- **API key auth** — uses `X-API-Key` header; configure in Copilot Studio's connection settings
- **7 operations per spec** — well under the ~10 operation limit where quality degrades

## API Base URL

Both specs target:

```
https://api.agentrails.io
```

This is the AgenticCommerce Enterprise API running on Render.
