# AgentRails Quick Reference

## Live URLs

| Resource | URL |
|----------|-----|
| Marketing Site | https://agentrails.io |
| Dashboard | https://app.agentrails.io |
| Production API | https://api.agentrails.io |
| Sandbox API | https://sandbox.agentrails.io |
| Docs | https://agentrails.io/docs |

## Local Development
```
Dashboard: https://localhost:7098/admin/index.html
API: https://localhost:7098/api
```

---

## Authentication

### Login
```bash
curl -X POST https://localhost:7098/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"password"}'
```

### Use Token
```bash
curl https://localhost:7098/api/agents \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### Use API Key
```bash
curl https://localhost:7098/api/agents \
  -H "X-API-Key: YOUR_API_KEY"
```

---

## Common Endpoints

### Signup & Account
| Action | Method | Endpoint |
|--------|--------|----------|
| Create Account | POST | `/api/signup` |
| Upgrade to PAYG | POST | `/api/signup/upgrade` |
| Check Status | GET | `/api/signup/status?email=...` |

### Billing & Usage
| Action | Method | Endpoint |
|--------|--------|----------|
| Get Usage (API key) | GET | `/api/billing/usage?days=30` |
| Get Usage (email) | GET | `/api/billing/usage/{email}` |
| Get Dashboard | GET | `/api/billing/dashboard` |

### x402 Payments
| Action | Method | Endpoint |
|--------|--------|----------|
| Get Pricing | GET | `/api/x402/pricing` |
| Protected Data | GET | `/api/x402/protected/data` |
| Protected Analysis | GET | `/api/x402/protected/analysis` |
| Verify Payment | POST | `/api/x402/facilitator/verify` |
| Settle Payment | POST | `/api/x402/facilitator/settle` |

### Trust & Discovery
| Action | Method | Endpoint |
|--------|--------|----------|
| Check Trust | GET | `/api/trust/check?serviceUrl=...` |
| Register Service | POST | `/api/trust/register` |
| Browse Registry | GET | `/api/trust/registry` |

### Agents
| Action | Method | Endpoint |
|--------|--------|----------|
| Health Check | GET | `/health` |
| List Agents | GET | `/api/agents` |
| Create Agent | POST | `/api/agents` |
| Delete Agent | DELETE | `/api/agents/{id}` |
| Agent Purchase | POST | `/api/agents/{id}/purchase` |
| Get Transactions | GET | `/api/agents/transactions` |

---

## Request Bodies

### Create Agent
```json
{
  "name": "Agent Name",
  "description": "What the agent does",
  "budget": 100.00
}
```

### Create Policy
```json
{
  "name": "Policy Name",
  "description": "Policy description",
  "maxTransactionAmount": 50.00,
  "dailySpendingLimit": 200.00,
  "requiresApproval": false,
  "enabled": true
}
```

### Make Purchase
```json
{
  "amount": 25.00,
  "recipientAddress": "0x1234...",
  "description": "Purchase description"
}
```

### Create API Key
```json
{
  "name": "Key Name"
}
```

---

## Dashboard Shortcuts

| Feature | How To |
|---------|--------|
| Sort Table | Click column header |
| Filter Data | Type in search box |
| Delete Item | Click trash icon, confirm |
| Create Item | Click "Create" button |
| Switch Section | Click sidebar menu |
| Logout | Click "Logout" in sidebar |

---

## Status Codes

| Code | Meaning |
|------|---------|
| 200 | Success |
| 201 | Created |
| 400 | Bad Request (validation error) |
| 401 | Unauthorized (login required) |
| 403 | Forbidden (no permission) |
| 404 | Not Found |
| 500 | Server Error |

---

## Log Levels

| Level | Color | Use |
|-------|-------|-----|
| Error | Red | Critical failures |
| Warning | Yellow | Potential issues |
| Information | Blue | Normal events |

---

## Useful URLs

### Production
| Resource | URL |
|----------|-----|
| Marketing Site | https://agentrails.io |
| Dashboard | https://app.agentrails.io |
| Production API | https://api.agentrails.io |
| Sandbox API | https://sandbox.agentrails.io |
| Swagger (Sandbox) | https://sandbox.agentrails.io/swagger |
| Swagger (Production) | https://api.agentrails.io/swagger |

### Local Development
| Resource | URL |
|----------|-----|
| Dashboard | `/admin/index.html` |
| Swagger UI | `/swagger` |
| Health | `/health` |
| API Status | `/` |
