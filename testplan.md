# Test Plan: AgenticCommerce x402 Payment System

## What Is This System?

Imagine you have a lemonade stand, but instead of people paying with cash, they pay with digital money (like coins in a video game, but real). This system lets computer programs (called "AI agents") buy things from websites using this digital money called "USDC" (which is worth the same as US dollars).

### The Magic Payment Process

1. **Someone asks for something** - Like asking for a glass of lemonade
2. **The system says "That'll be 1 cent please"** - It sends back a "payment required" message
3. **The person signs a digital check** - They use a special digital signature (like signing your name, but with math)
4. **The system checks the signature** - Makes sure it's really from them
5. **Everyone is happy** - The person gets their lemonade, the stand gets paid

---

## Before You Start Testing

### What You Need

1. **A web browser** (Chrome, Firefox, or Edge)
2. **The API running** (we'll show you how)
3. **PostgreSQL database running** (you said it's already running - great!)

### Starting the API Server

1. Open a **Command Prompt** or **Terminal** window
2. Type these commands one at a time, pressing Enter after each:

```
cd C:\AgenticCommerce\AgenticCommerce\src\AgenticCommerce.API
dotnet run
```

3. Wait until you see a message that looks like:
```
Now listening on: http://localhost:5000
Now listening on: https://localhost:5001
```

4. **Keep this window open!** If you close it, the server stops.

### Opening Swagger (The Testing Tool)

1. Open your web browser
2. Go to this address: **http://localhost:5000/swagger**
3. You should see a page titled "AgenticCommerce API" with lots of colorful boxes

If you see an error, try: **https://localhost:5001/swagger** instead (note the 's' in https)

---

## Understanding Swagger

Swagger is like a remote control for our system. Each colored box is a button that does something different.

### The Colors Mean:

| Color | What It Does |
|-------|--------------|
| **Blue (GET)** | Asks for information (like asking "what's on the menu?") |
| **Green (POST)** | Sends information (like placing an order) |
| **Yellow (PUT)** | Updates information |
| **Red (DELETE)** | Removes something |

### How to Use a Swagger Button:

1. **Click on the colored bar** to expand it
2. **Click the "Try it out" button** (top right of the expanded section)
3. **Fill in any required information** (if there are empty boxes)
4. **Click the big blue "Execute" button**
5. **Look at the "Response"** section below to see what happened

---

## Test 1: Check If The System Is Alive

**What we're testing:** Does the server respond at all?

### Steps:

1. In Swagger, find the section called **"Transactions"**
2. Click on **GET /api/transactions/status**
3. Click **"Try it out"**
4. Click **"Execute"**

### What Success Looks Like:

- **Response code: 200** (shown in a box)
- Response body shows something like:
```json
{
  "arcConnected": true,
  "walletAddress": "0x..."
}
```

### What Failure Looks Like:

- Response code is NOT 200 (might be 500, 404, etc.)
- Error message in red

**Write down:** [ ] Passed  [ ] Failed - Notes: _________________

---

## Test 2: Check Wallet Balance

**What we're testing:** Can the system see how much digital money it has?

### Steps:

1. Find the section called **"Transactions"**
2. Click on **GET /api/transactions/balance**
3. Click **"Try it out"**
4. Click **"Execute"**

### What Success Looks Like:

- **Response code: 200**
- Response body shows a balance (might be 0, that's OK):
```json
{
  "balance": 10.5,
  "currency": "USDC",
  "walletAddress": "0x..."
}
```

### What Failure Looks Like:

- Response code 500 or other error
- Message saying "failed to get balance"

**Write down:** [ ] Passed  [ ] Failed - Notes: _________________

---

## Test 3: View Payment Pricing Information

**What we're testing:** Can we see how much things cost?

### Steps:

1. Find the section called **"X402"** (this is the payment system)
2. Click on **GET /api/x402/pricing**
3. Click **"Try it out"**
4. Click **"Execute"**

### What Success Looks Like:

- **Response code: 200**
- Response shows pricing information:
```json
{
  "endpoints": [
    {
      "path": "/api/x402/protected/analysis",
      "priceUsdc": 0.01,
      "description": "AI market analysis"
    }
  ]
}
```

### What Failure Looks Like:

- Response code is not 200
- Empty or error response

**Write down:** [ ] Passed  [ ] Failed - Notes: _________________

---

## Test 4: Try to Access a Paid Endpoint (Without Paying)

**What we're testing:** Does the system correctly ask for payment?

### Steps:

1. Find the section called **"X402"**
2. Click on **GET /api/x402/protected/analysis**
3. Click **"Try it out"**
4. Click **"Execute"**

### What Success Looks Like:

- **Response code: 402** (this means "Payment Required" - that's what we want!)
- Response headers include **"x-payment-required"**
- Response body shows payment requirements:
```json
{
  "x402Version": 2,
  "accepts": [
    {
      "scheme": "exact",
      "network": "arc-testnet",
      "maxAmountRequired": "10000",
      "payTo": "0x..."
    }
  ]
}
```

### What Failure Looks Like:

- Response code 200 (means it let you in without paying - bad!)
- Response code 500 (server error)
- No payment requirements in response

**Write down:** [ ] Passed  [ ] Failed - Notes: _________________

---

## Test 5: Check Wallet Status (Development)

**What we're testing:** Can we see the wallet connection and balance?

### Steps:

1. Find the section called **"X402"**
2. Click on **GET /api/x402/test/wallet-status**
3. Click **"Try it out"**
4. Click **"Execute"**

### What Success Looks Like:

- **Response code: 200**
- Response shows wallet information:
```json
{
  "walletAddress": "0x...",
  "connected": true,
  "balance": 10.5,
  "network": "arc-testnet"
}
```

### What Failure Looks Like:

- Response code is not 200
- Connection error or missing wallet info

**Write down:** [ ] Passed  [ ] Failed - Notes: _________________

---

## Test 6: Verify a Payment Payload (Facilitator Test)

**What we're testing:** Can the system check if a payment signature is valid?

This is like checking if someone's signature on a check is real.

### Part A: Generate a REAL Signed Payload

First, we need to get a real cryptographic signature:

1. Find the section called **"X402"**
2. Click on **GET /api/x402/test/generate-signed-payload**
3. Click **"Try it out"**
4. Click **"Execute"**
5. In the response, find the **"verifyRequestBody"** section
6. **Copy the ENTIRE verifyRequestBody object** (everything inside the curly braces including the braces)

It will look something like this (but with different values):
```json
{
  "paymentPayload": {
    "x402Version": 2,
    "scheme": "exact",
    "network": "base-sepolia",
    "payload": {
      "signature": "0x1234...actual signature...",
      "authorization": {...}
    }
  },
  "paymentRequirements": {...}
}
```

### Part B: Verify the Signed Payload

Now use that payload to test verification:

1. Click on **POST /api/x402/facilitator/verify**
2. Click **"Try it out"**
3. **Paste** the verifyRequestBody you copied from Part A
4. Click **"Execute"**

### What Success Looks Like:

- **Response code: 200**
- Response shows:
```json
{
  "isValid": true,
  "payer": "0xf39Fd6e51aad88F6F4ce6aB8827279cffFb92266"
}
```

**`isValid: true`** means the cryptographic signature was verified successfully!

### What Failure Looks Like:

- Response code 400 or 500
- `isValid: false` (signature didn't match)
- Error message about JSON parsing

**Write down:** [ ] Passed  [ ] Failed - Notes: _________________

---

## Test 7: View Payment History

**What we're testing:** Can we see past payments?

### Steps:

1. Find the section called **"X402"**
2. Click on **GET /api/x402/payments**
3. Click **"Try it out"**
4. Click **"Execute"**

### What Success Looks Like:

- **Response code: 200**
- Response shows a list (might be empty, that's OK):
```json
{
  "payments": [],
  "totalCount": 0
}
```

OR if there are payments:
```json
{
  "payments": [
    {
      "paymentId": "...",
      "amount": "10000",
      "status": "settled"
    }
  ],
  "totalCount": 1
}
```

### What Failure Looks Like:

- Response code 500
- Database error message

**Write down:** [ ] Passed  [ ] Failed - Notes: _________________

---

## Test 8: View Payment Statistics

**What we're testing:** Can we see summary numbers about payments?

### Steps:

1. Find the section called **"X402"**
2. Click on **GET /api/x402/stats**
3. Click **"Try it out"**
4. Click **"Execute"**

### What Success Looks Like:

- **Response code: 200**
- Response shows statistics:
```json
{
  "totalPayments": 0,
  "totalVolumeUsdc": 0,
  "averagePaymentUsdc": 0
}
```

### What Failure Looks Like:

- Response code 500
- Error message

**Write down:** [ ] Passed  [ ] Failed - Notes: _________________

---

## Test 9: Create an AI Agent

**What we're testing:** Can we create a new AI agent that can make payments?

### Steps:

1. Find the section called **"Agents"**
2. Click on **POST /api/agents**
3. Click **"Try it out"**
4. In the text box, paste:

```json
{
  "name": "Test Agent",
  "description": "A test agent for testing",
  "budget": 10.00,
  "capabilities": ["research", "payments"]
}
```

5. Click **"Execute"**

### What Success Looks Like:

- **Response code: 200** or **201**
- Response shows the created agent:
```json
{
  "id": "agent_abc123...",
  "name": "Test Agent",
  "description": "A test agent for testing",
  "budget": 10.00,
  "currentBalance": 10.00,
  "status": "Active",
  "walletAddress": "0x..."
}
```

**IMPORTANT:** Write down the `id` value! You'll need it for the next tests.

Agent ID: ____________________________

### What Failure Looks Like:

- Response code 400 (bad request) or 500 (server error)
- Error message about validation

**Write down:** [ ] Passed  [ ] Failed - Notes: _________________

---

## Test 10: Get Agent Information

**What we're testing:** Can we look up the agent we just created?

### Steps:

1. Find the section called **"Agents"**
2. Click on **GET /api/agents/{id}**
3. Click **"Try it out"**
4. In the **"id"** box, paste the agent ID you wrote down in Test 9
5. Click **"Execute"**

### What Success Looks Like:

- **Response code: 200**
- Response shows the agent information (same as what we created)

### What Failure Looks Like:

- Response code 404 (not found)
- Response code 500 (server error)

**Write down:** [ ] Passed  [ ] Failed - Notes: _________________

---

## Test 11: List All Agents

**What we're testing:** Can we see all the agents?

### Steps:

1. Find the section called **"Agents"**
2. Click on **GET /api/agents**
3. Click **"Try it out"**
4. Click **"Execute"**

### What Success Looks Like:

- **Response code: 200**
- Response shows a list with at least the agent we created:
```json
[
  {
    "id": "agent_abc123...",
    "name": "Test Agent",
    ...
  }
]
```

### What Failure Looks Like:

- Response code 500
- Empty response when we know we created an agent

**Write down:** [ ] Passed  [ ] Failed - Notes: _________________

---

## Test 12: Delete an Agent

**What we're testing:** Can we remove an agent?

### Steps:

1. Find the section called **"Agents"**
2. Click on **DELETE /api/agents/{id}**
3. Click **"Try it out"**
4. In the **"id"** box, paste the agent ID from Test 9
5. Click **"Execute"**

### What Success Looks Like:

- **Response code: 200** or **204**
- The agent is deleted

### Verify It's Really Gone:

1. Go back to **GET /api/agents/{id}**
2. Try to get the same agent ID
3. You should get **Response code: 404** (not found)

### What Failure Looks Like:

- Response code 500
- Agent still exists after deletion

**Write down:** [ ] Passed  [ ] Failed - Notes: _________________

---

## Test 13: Test Arc Network in Payment Requirements

**What we're testing:** Does the protected endpoint offer Arc as a payment network?

### Steps:

1. Find the section called **"X402"**
2. Click on **GET /api/x402/protected/analysis**
3. Click **"Try it out"**
4. Click **"Execute"**

### What to Check:

Look in the 402 response for:
- The `network` field shows `"arc-testnet"`
- The `payTo` address starts with `0x`
- The `asset` field has the USDC contract address

### What Success Looks Like:

- **Response code: 402**
- Response includes Arc network configuration:
```json
{
  "accepts": [
    {
      "network": "arc-testnet",
      "payTo": "0x...",
      "asset": "0x3600000000000000000000000000000000000000"
    }
  ]
}
```

### What Failure Looks Like:

- Network is not "arc-testnet"
- Missing payTo or asset fields

**Write down:** [ ] Passed  [ ] Failed - Notes: _________________

---

## Test 14: Test Multi-Network Endpoint

**What we're testing:** Does the system support multiple blockchain networks?

### Steps:

1. Find the section called **"X402Example"**
2. Click on **GET /api/x402-example/multichain**
3. Click **"Try it out"**
4. Click **"Execute"**

### What Success Looks Like:

- **Response code: 402** (Payment Required)
- Response shows multiple network options in the `accepts` array:
```json
{
  "accepts": [
    { "network": "arc-testnet", ... },
    { "network": "base-sepolia", ... }
  ]
}
```

### What Failure Looks Like:

- Only one network shown
- Response code 500

**Write down:** [ ] Passed  [ ] Failed - Notes: _________________

---

## Test 15: Test Generate Payment Payload

**What we're testing:** Can the system generate a test payment payload?

### Steps:

1. Find the section called **"X402"**
2. Click on **GET /api/x402/test/generate-payload**
3. Click **"Try it out"**
4. Click **"Execute"**

### What Success Looks Like:

- **Response code: 200**
- Response shows a generated payment payload:
```json
{
  "paymentPayload": {
    "x402Version": 2,
    "scheme": "exact",
    "network": "arc-testnet",
    "payload": {
      "signature": "0x...",
      "authorization": {...}
    }
  }
}
```

### What Failure Looks Like:

- Response code 500
- Missing or invalid payload structure

**Write down:** [ ] Passed  [ ] Failed - Notes: _________________

---

## Test Summary Sheet

Fill this out after completing all tests:

| Test # | Test Name | Result | Notes |
|--------|-----------|--------|-------|
| 1 | System Status Check | [ ] Pass [ ] Fail | |
| 2 | Wallet Balance | [ ] Pass [ ] Fail | |
| 3 | Payment Pricing | [ ] Pass [ ] Fail | |
| 4 | Protected Endpoint (402) | [ ] Pass [ ] Fail | |
| 5 | Wallet Status (Dev) | [ ] Pass [ ] Fail | |
| 6 | Verify Payment Payload | [ ] Pass [ ] Fail | |
| 7 | Payment History | [ ] Pass [ ] Fail | |
| 8 | Payment Statistics | [ ] Pass [ ] Fail | |
| 9 | Create Agent | [ ] Pass [ ] Fail | |
| 10 | Get Agent | [ ] Pass [ ] Fail | |
| 11 | List Agents | [ ] Pass [ ] Fail | |
| 12 | Delete Agent | [ ] Pass [ ] Fail | |
| 13 | Arc Network in Payments | [ ] Pass [ ] Fail | |
| 14 | Multi-Network Support | [ ] Pass [ ] Fail | |
| 15 | Generate Payment Payload | [ ] Pass [ ] Fail | |

**Total Passed:** _____ / 15

**Tester Name:** _____________________

**Date Tested:** _____________________

**Overall Result:** [ ] All Tests Passed  [ ] Some Tests Failed

---

## If Something Goes Wrong

### The Server Won't Start

1. Make sure PostgreSQL is running
2. Check if another program is using port 5000 or 5001
3. Try closing and reopening the command prompt

### Swagger Won't Load

1. Make sure the server is running (see above)
2. Try refreshing the page
3. Try the other URL (http vs https)

### A Test Fails

1. Write down the exact error message
2. Take a screenshot if possible
3. Note which test number failed
4. Continue with the other tests

### You See "500 Internal Server Error"

This means something went wrong inside the system. Write down:
1. Which endpoint you were testing
2. What data you sent (if any)
3. The full error message

---

## Glossary (Word Definitions)

| Word | What It Means |
|------|---------------|
| **API** | Application Programming Interface - a way for programs to talk to each other |
| **Swagger** | A tool that lets you test APIs in your web browser |
| **Endpoint** | A specific URL that does something (like /api/agents) |
| **Response Code** | A number that tells you if something worked (200 = good, 400/500 = bad) |
| **JSON** | A way of organizing data that looks like `{"name": "value"}` |
| **USDC** | A type of digital money worth $1 USD each |
| **x402** | The payment system that asks for money before giving you access |
| **Agent** | A computer program that can do tasks and make payments |
| **Wallet** | Where digital money is stored (like a digital purse) |
| **Arc** | Circle's new blockchain network where payments happen |
| **Blockchain** | A digital record book that everyone can trust |

---

## Questions?

If you have questions about this test plan, please ask the developer who gave it to you!
