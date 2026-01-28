# AgentRails Quick Reference

## Dashboard URL
```
https://localhost:7098/admin/index.html
```

## API Base URL
```
https://localhost:7098/api
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

| Action | Method | Endpoint |
|--------|--------|----------|
| Health Check | GET | `/health` |
| List Agents | GET | `/api/agents` |
| Create Agent | POST | `/api/agents` |
| Delete Agent | DELETE | `/api/agents/{id}` |
| Agent Purchase | POST | `/api/agents/{id}/purchase` |
| List Policies | GET | `/api/policies` |
| Create Policy | POST | `/api/policies` |
| Get Transactions | GET | `/api/agents/transactions` |
| List API Keys | GET | `/api/auth/api-keys` |
| Create API Key | POST | `/api/auth/api-keys` |
| Get Logs | GET | `/api/logs` |

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

| Resource | URL |
|----------|-----|
| Dashboard | `/admin/index.html` |
| Swagger UI | `/swagger` |
| Health | `/health` |
| API Status | `/` |
