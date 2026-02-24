# Microsoft Marketplace Listing — AgentRails

> Prepared for Partner Center submission. Last updated: Feb 24, 2026.

## Pre-Submission Checklist

- [ ] Register at [Microsoft Partner Center](https://partner.microsoft.com/) with @agentrails.io email
- [ ] Enroll in **Microsoft AI Cloud Partner Program** (free)
- [ ] Enroll in **Commercial Marketplace program** (for SaaS offer)
- [ ] Enroll in **ISV Success Program** (free, $126K in benefits)
- [ ] Prepare privacy policy page (https://www.agentrails.io/privacy)
- [ ] Prepare terms of use page (https://www.agentrails.io/terms)
- [ ] Create logo assets (216x216, 90x90, 48x48 PNG)
- [ ] Create 3-5 screenshots (1280x720 PNG)
- [ ] Record demo video (3+ min, YouTube or Vimeo)

---

# LISTING 1: SaaS Offer — AgentRails

## Offer Identity

| Field | Value |
|-------|-------|
| **Offer ID** | `agentrails` |
| **Offer Alias** | AgentRails |
| **Offer Type** | SaaS |
| **Listing Option** | Contact Me (initially) → Free Trial (after SaaS integration) |

## Listing Details

### Name (Offer Title)
```
AgentRails — AI Agent Payment Infrastructure for x402
```
(55 characters)

### Search Results Summary
```
Build, govern, and pay AI agents on Microsoft 365 with x402 autonomous payments.
```
(82 characters — limit: 100)

### Description (HTML)
```html
<h3>Zero Trust payment infrastructure for AI agents</h3>

<p>Microsoft's Cyber Pulse report (Feb 2026) found that 80% of Fortune 500 deploy AI agents — but only 47% have security controls for them. AgentRails closes that governance gap with x402 payment infrastructure that enforces Zero Trust principles at the protocol level: least privilege via per-request payments, explicit verification via cryptographic proof, and full observability via on-chain audit trails.</p>

<h3>Security &amp; governance first</h3>

<ul>
<li><strong>Per-agent spending limits and budget caps</strong> — each agent gets individual budgets with hard and soft limits</li>
<li><strong>Kill switches for compromised agents</strong> — immediately revoke an agent's ability to spend</li>
<li><strong>Approval workflows</strong> — human-in-the-loop for high-value transactions</li>
<li><strong>Role-based access control</strong> — Admin, Finance Manager, Agent Operator, Auditor, Read-Only</li>
<li><strong>Observability</strong> — every agent action, payment, and policy change logged with timestamps and on-chain transaction hashes</li>
<li><strong>No standing credentials</strong> — agents pay per-request via signed USDC authorizations, eliminating API key sprawl and rotation</li>
</ul>

<h3>How it works</h3>

<ol>
<li><strong>Agent calls any API</strong> — No signup required. Standard HTTP request. Server returns 402 with the price.</li>
<li><strong>Agent pays instantly</strong> — SDK checks budget limits, signs a USDC payment authorization, and retries in one round trip.</li>
<li><strong>Data returned, settled on-chain</strong> — API delivers the response. Payment settles with cryptographic proof. Agent moves on.</li>
</ol>

<h3>5 SDK integrations</h3>

<ul>
<li><strong>LangChain</strong> (Python) — pip install langchain-x402</li>
<li><strong>CrewAI</strong> (Python) — pip install crewai-x402</li>
<li><strong>Semantic Kernel</strong> (.NET) — dotnet add package AgentRails.SemanticKernel.X402</li>
<li><strong>Microsoft Agent Framework</strong> (.NET) — dotnet add package AgentRails.AgentFramework.X402</li>
<li><strong>Copilot Studio</strong> — Import OpenAPI spec for Microsoft Teams integration</li>
</ul>

<h3>Built on trusted infrastructure</h3>

<p>AgentRails integrates with Microsoft Power Platform, Copilot Studio, Circle (USDC), Coinbase (Base network), and the x402 open protocol. Deploy on your own infrastructure or use our hosted facilitator.</p>

<h3>Pricing</h3>

<ul>
<li><strong>Sandbox</strong> — Free. Full API access on testnet with test USDC.</li>
<li><strong>Pay-as-you-go</strong> — 0.5% per transaction on mainnet. No monthly minimum.</li>
<li><strong>Pro</strong> — $49/month. Unlimited transactions, 0% fees, priority support.</li>
<li><strong>Enterprise</strong> — $2,500 one-time. Full source code, policy engine, admin dashboard, Copilot Studio plugins.</li>
</ul>
```
(~2,800 characters — limit: 5,000)

### Getting Started Instructions
```
Getting started with AgentRails takes minutes:

1. Try the Sandbox: Visit https://sandbox.agentrails.io/swagger to explore the full API on testnet — no signup required. Use test USDC to make x402 payments.

2. Install an SDK: Add x402 payments to your agents in one line:
   - Python (LangChain): pip install langchain-x402
   - Python (CrewAI): pip install crewai-x402
   - .NET (Semantic Kernel): dotnet add package AgentRails.SemanticKernel.X402
   - .NET (Agent Framework): dotnet add package AgentRails.AgentFramework.X402
   - Copilot Studio: Import the OpenAPI spec from our docs

3. Read the docs: Full integration guides, tutorials, and API reference at https://www.agentrails.io/docs

4. Go to production: When ready for mainnet, contact sales@agentrails.io to activate your account with real USDC on Base, Ethereum, or Arc networks.

For enterprise deployments with Copilot Studio, Power Automate, and governance policies, book a demo at sales@agentrails.io.
```
(~950 characters — limit: 3,000)

### Search Keywords
```
x402, AI agent payments, autonomous payments, AI agent governance, AI agent security, zero trust AI agents, AI agent observability
```
(7 keywords)

### Privacy Policy Link
```
https://www.agentrails.io/privacy
```

### Terms of Use Link
```
https://www.agentrails.io/terms
```

### Support Contact
| Field | Value |
|-------|-------|
| Name | AgentRails Support |
| Email | support@agentrails.io |
| Phone | (on file) |
| Support URL | https://www.agentrails.io/docs |

### Engineering Contact
| Field | Value |
|-------|-------|
| Name | Kevin Matthews |
| Email | kevin@agentrails.io |
| Phone | (on file) |

### Categories

**Primary Category:** AI Apps and Agents → Tools & Connectors

**Secondary Category:** Finance → Payments/Credit/Collections

### Industries

**Primary:** Financial Services → Banking

**Secondary:** Professional Services → Consulting

### Useful Links

| Title | URL |
|-------|-----|
| Documentation | https://www.agentrails.io/docs |
| Sandbox (Swagger API) | https://sandbox.agentrails.io/swagger |
| GitHub — Core Platform | https://github.com/kmatthewsio/AgenticCommerce |
| GitHub — LangChain x402 SDK | https://github.com/kmatthewsio/langchain-x402 |
| Blog | https://www.agentrails.io/blog |
| Blog: Zero Trust for AI Agents | https://www.agentrails.io/blog/zero-trust-for-ai-agents.html |

### Supporting Documents (PDFs to create)

1. **AgentRails Platform Overview** — 2-page product brief covering x402 protocol, SDK integrations, governance, pricing
2. **Enterprise Architecture Guide** — Technical overview of Copilot Studio integration, MCP servers, Power Platform deployment
3. **x402 Protocol Whitepaper** — Protocol specification, payment flow, security model

## Logo Assets (to create)

| Size | Dimensions | Filename |
|------|-----------|----------|
| Large | 300x300 px | `marketplace/logo-300x300.png` |
| Medium | 90x90 px | `marketplace/logo-90x90.png` |
| Small | 48x48 px | `marketplace/logo-48x48.png` |

## Screenshots (to create — 1280x720 PNG each)

1. **Sandbox API Explorer** — Swagger UI showing x402 payment endpoints with 402 response example
2. **SDK Integration** — Code snippet showing LangChain agent making an x402 payment in 3 lines
3. **Enterprise Dashboard** — Admin dashboard showing agent management, transaction history, policy controls
4. **Copilot Studio in Teams** — Finance team querying x402 revenue via Copilot in Microsoft Teams
5. **Payment Flow** — Visual diagram of the 402 → Pay → Settle → Respond flow

## Demo Video (to create)

| Field | Value |
|-------|-------|
| Title | AgentRails — AI Agent Payment Infrastructure Demo |
| Length | 3-5 minutes |
| Platform | YouTube (unlisted or public) |
| Thumbnail | 1280x720 PNG |

**Script outline:**
1. (0:00-0:30) Problem: AI agents can't pay for APIs autonomously
2. (0:30-1:30) Demo: Agent calls API → gets 402 → pays USDC → gets data
3. (1:30-2:30) Enterprise: Copilot Studio plugins, governance dashboard, audit trails
4. (2:30-3:30) SDK showcase: LangChain, CrewAI, Semantic Kernel, Agent Framework
5. (3:30-4:00) CTA: Try sandbox, book demo

---

# LISTING 2: Consulting Service

## Offer Identity

| Field | Value |
|-------|-------|
| **Offer ID** | `agentrails-consulting` |
| **Offer Alias** | AgentRails Consulting |
| **Offer Type** | Consulting Service |
| **Listing Option** | Contact Me |

## Listing Details

### Name (Title — must follow "Name: Duration Type" format)
```
AgentRails AI Agent Architecture: 2-Week Assessment
```

### Search Results Summary
```
Expert architecture for AI agents on Microsoft 365 with Copilot Studio and x402 payments.
```
(90 characters — limit: 100)

### Description (Markdown)
```markdown
## Your AI agents need infrastructure. We build it.

AgentRails provides expert AI agent architecture consulting for enterprises on the Microsoft stack. From Copilot Studio buildout to MCP server development to agent governance — we architect the full AI agent lifecycle on your Microsoft 365 environment.

## What you get

### Week 1: Discovery & Architecture

- **M365 environment audit** — Assess current Copilot, Power Platform, and Dataverse configuration
- **Agent use case mapping** — Identify high-value agent opportunities across your organization
- **Governance framework design** — Roles, policies, approval chains, spending limits, and kill switches
- **MCP server architecture** — Design Model Context Protocol servers for secure agent access to internal data

### Week 2: Build & Deploy

- **Copilot Studio agent buildout** — Build and deploy custom AI agents with governed capabilities
- **MCP server development** — Implement MCP servers that give agents scoped access to internal APIs and tools
- **Power Automate integration** — Orchestrate agent workflows, approval chains, and compliance reporting
- **x402 payment layer** — Enable autonomous agent payments with USDC settlement and enterprise controls
- **Dataverse configuration** — Agent state, governance policies, audit logs, and Power BI dashboards

## Deliverables

- Copilot Studio agents deployed to Microsoft Teams
- MCP server(s) connected to your internal systems
- Power Automate governance flows
- Dataverse tables with security roles (System Admin, Finance Manager, Agent Operator, Auditor, Read-Only)
- x402 payment infrastructure (optional — for agent-to-agent or agent-to-API payments)
- Architecture documentation and runbook

## Who this is for

Organizations on Microsoft 365 that want to deploy AI agents with proper governance, compliance, and (optionally) autonomous payment capabilities. Ideal for finance, operations, and IT teams exploring Copilot Studio and the Power Platform for AI agent deployment.

## Technologies

Microsoft Copilot Studio, Power Platform (Power Automate, Power Apps, Dataverse), Model Context Protocol (MCP), x402 Payment Protocol, USDC/Circle, Microsoft Teams
```
(~2,100 characters — limit: 3,000)

### Service Type
```
Assessment
```

### Applicable Products
```
Microsoft 365, Microsoft Copilot Studio, Microsoft Power Platform
```

### Categories

**Primary Category:** AI Apps and Agents → Agents

**Secondary Category:** IT & Management Tools → Business Applications

### Industries

**Primary:** Financial Services

**Secondary:** Professional Services

### Pricing
```
Contact for pricing
```

### Duration
```
2 weeks
```

---

# Asset Creation Checklist

## Required Before Submission

### Legal Pages (to create on www.agentrails.io)
- [ ] `/privacy` — Privacy policy page
- [ ] `/terms` — Terms of use page

### Logo Assets
- [ ] `marketplace/logo-300x300.png` — Square logo, AgentRails branding
- [ ] `marketplace/logo-90x90.png` — Medium logo
- [ ] `marketplace/logo-48x48.png` — Small logo

### Screenshots (1280x720 PNG)
- [ ] Screenshot 1: Sandbox API Explorer
- [ ] Screenshot 2: SDK Integration code
- [ ] Screenshot 3: Enterprise Dashboard
- [ ] Screenshot 4: Copilot Studio in Teams
- [ ] Screenshot 5: Payment Flow diagram

### PDFs
- [ ] AgentRails Platform Overview (2-page brief)
- [ ] Enterprise Architecture Guide
- [ ] x402 Protocol Whitepaper

### Video
- [ ] Demo video (3-5 min, YouTube)
- [ ] Video thumbnail (1280x720 PNG)

## Nice-to-Have
- [ ] Customer testimonial / case study
- [ ] Integration architecture diagram
- [ ] ROI calculator or comparison chart

---

# Submission Timeline

| Step | Action | Est. Duration |
|------|--------|---------------|
| 1 | Register Partner Center + enroll in programs | 1-2 days |
| 2 | Create privacy/terms pages on site | 1 day |
| 3 | Create logo, screenshot, and PDF assets | 2-3 days |
| 4 | Record demo video | 1-2 days |
| 5 | Submit SaaS offer in Partner Center | 1 day |
| 6 | Submit Consulting Service offer | 1 day |
| 7 | Microsoft certification review | 1-4 weeks |
| 8 | Preview review + go live | 1-2 days |

**Total: ~2-5 weeks from start to live listing**
