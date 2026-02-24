"""
Generate Microsoft Marketplace PDF documents for AgentRails.
1. Platform Overview (2-page brief)
2. Enterprise Architecture Guide
3. x402 Protocol Whitepaper
"""

from fpdf import FPDF
import os

OUT = os.path.dirname(os.path.abspath(__file__))

# Brand colors (RGB)
INDIGO = (79, 70, 229)
CYAN = (6, 182, 212)
DARK = (24, 24, 27)
GRAY = (82, 82, 91)
LIGHT_GRAY = (113, 113, 122)
WHITE = (255, 255, 255)
BG_LIGHT = (248, 250, 252)


class BrandedPDF(FPDF):
    """Base PDF with AgentRails branding."""

    def __init__(self, title_text=""):
        super().__init__()
        self.title_text = title_text
        self.set_auto_page_break(auto=True, margin=25)

    def header(self):
        if self.page_no() == 1:
            return  # Cover page has its own header
        self.set_font("Helvetica", "B", 9)
        self.set_text_color(*LIGHT_GRAY)
        self.cell(0, 8, "AgentRails", align="L")
        self.cell(0, 8, self.title_text, align="R", new_x="LMARGIN", new_y="NEXT")
        self.set_draw_color(*INDIGO)
        self.set_line_width(0.5)
        self.line(10, self.get_y(), 200, self.get_y())
        self.ln(6)

    def footer(self):
        self.set_y(-15)
        self.set_font("Helvetica", "", 8)
        self.set_text_color(*LIGHT_GRAY)
        self.cell(0, 10, f"www.agentrails.io  |  Page {self.page_no()}", align="C")

    def cover_page(self, title, subtitle, date="February 2026"):
        self.add_page()
        # Gradient-like header bar
        self.set_fill_color(*INDIGO)
        self.rect(0, 0, 210, 100, "F")
        self.set_fill_color(50, 55, 200)
        self.rect(0, 0, 210, 4, "F")

        # Logo area
        self.set_xy(20, 25)
        self.set_font("Helvetica", "B", 28)
        self.set_text_color(*WHITE)
        self.cell(0, 12, ">_ AgentRails", new_x="LMARGIN", new_y="NEXT")

        # Title
        self.set_xy(20, 50)
        self.set_font("Helvetica", "B", 22)
        self.multi_cell(170, 10, title)

        # Subtitle
        self.set_xy(20, 78)
        self.set_font("Helvetica", "", 12)
        self.set_text_color(200, 200, 240)
        self.cell(0, 8, subtitle, new_x="LMARGIN", new_y="NEXT")

        # Date and URL
        self.set_xy(20, 110)
        self.set_font("Helvetica", "", 10)
        self.set_text_color(*LIGHT_GRAY)
        self.cell(0, 6, date, new_x="LMARGIN", new_y="NEXT")
        self.cell(0, 6, "www.agentrails.io", new_x="LMARGIN", new_y="NEXT")

    def section_heading(self, text):
        self.ln(4)
        self.set_font("Helvetica", "B", 14)
        self.set_text_color(*INDIGO)
        self.cell(0, 10, text, new_x="LMARGIN", new_y="NEXT")
        self.set_draw_color(*INDIGO)
        self.set_line_width(0.3)
        self.line(10, self.get_y(), 80, self.get_y())
        self.ln(3)

    def sub_heading(self, text):
        self.ln(2)
        self.set_font("Helvetica", "B", 11)
        self.set_text_color(*DARK)
        self.cell(0, 8, text, new_x="LMARGIN", new_y="NEXT")

    def body_text(self, text):
        self.set_font("Helvetica", "", 10)
        self.set_text_color(*GRAY)
        self.multi_cell(0, 5.5, text)
        self.ln(2)

    def bullet(self, text, bold_prefix=""):
        self.set_font("Helvetica", "", 10)
        self.set_text_color(*GRAY)
        x = self.get_x()
        self.cell(6, 5.5, "-")
        if bold_prefix:
            self.set_font("Helvetica", "B", 10)
            self.set_text_color(*DARK)
            self.write(5.5, bold_prefix + " ")
            self.set_font("Helvetica", "", 10)
            self.set_text_color(*GRAY)
            remaining = text[len(bold_prefix):].lstrip(" \u2014\u2013-")
            if remaining.startswith(" "):
                remaining = remaining[1:]
            self.multi_cell(0, 5.5, "-- " + remaining if remaining else "")
        else:
            self.multi_cell(0, 5.5, text)
        self.ln(1)

    def info_box(self, title, content):
        self.ln(2)
        y_start = self.get_y()
        self.set_fill_color(*BG_LIGHT)
        self.set_draw_color(200, 200, 210)
        # Estimate height
        self.rect(10, y_start, 190, 30, "DF")
        self.set_xy(14, y_start + 3)
        self.set_font("Helvetica", "B", 10)
        self.set_text_color(*INDIGO)
        self.cell(0, 6, title, new_x="LMARGIN", new_y="NEXT")
        self.set_x(14)
        self.set_font("Helvetica", "", 9)
        self.set_text_color(*GRAY)
        self.multi_cell(182, 5, content)
        box_h = self.get_y() - y_start + 4
        # Redraw box with correct height
        self.set_fill_color(*BG_LIGHT)
        self.set_draw_color(200, 200, 210)
        self.rect(10, y_start, 190, box_h, "D")
        self.ln(4)


# =============================================================================
# PDF 1: Platform Overview
# =============================================================================

def generate_platform_overview():
    pdf = BrandedPDF("Platform Overview")
    pdf.cover_page(
        "Platform Overview",
        "AI Agent Payment Infrastructure for the Enterprise",
    )

    # Page 2
    pdf.add_page()

    pdf.section_heading("What is AgentRails?")
    pdf.body_text(
        "AgentRails is enterprise AI agent infrastructure that gives your agents autonomous "
        "payment capabilities via the x402 protocol. Build with Microsoft Copilot Studio, "
        "govern with policies and audit trails, pay with USDC stablecoin -- from development "
        "to production, guardrails included."
    )
    pdf.body_text(
        "The x402 protocol replaces API keys with HTTP-native payments. When an agent calls a "
        "protected API, the server returns 402 Payment Required with the price. The agent pays "
        "instantly in USDC and gets access -- no signup, no credentials, no subscription tiers."
    )

    pdf.section_heading("Key Capabilities")
    pdf.bullet("5 SDK integrations -- LangChain, CrewAI, Semantic Kernel, Microsoft Agent Framework, and Copilot Studio", "5 SDK integrations")
    pdf.bullet("Enterprise governance -- Per-agent spending limits, approval workflows, kill switches, and full audit trails", "Enterprise governance")
    pdf.bullet("Microsoft 365 native -- Copilot Studio plugins let finance teams query x402 revenue in Teams", "Microsoft 365 native")
    pdf.bullet("Instant USDC settlement -- Payments settle in milliseconds on-chain with cryptographic proof", "Instant USDC settlement")
    pdf.bullet("Zero credential management -- No API keys to provision, rotate, or leak", "Zero credential management")
    pdf.bullet("Open source foundation -- Core protocol on GitHub, enterprise features layered on top", "Open source foundation")

    pdf.section_heading("How It Works")
    pdf.sub_heading("1. Agent Calls Any API")
    pdf.body_text("No signup required. Standard HTTP request. The server returns 402 Payment Required with the price in USDC.")
    pdf.sub_heading("2. Agent Pays Instantly")
    pdf.body_text("The SDK checks budget limits, signs a USDC payment authorization (EIP-3009), and retries the request -- all in one round trip. No human approval needed.")
    pdf.sub_heading("3. Data Returned, Settled On-Chain")
    pdf.body_text("The API delivers the response. Payment settles in milliseconds with cryptographic proof on Base, Ethereum, or Arc. The agent moves to the next task.")

    pdf.section_heading("Pricing")
    pdf.bullet("Sandbox -- Free. Full API access on testnet with test USDC.", "Sandbox")
    pdf.bullet("Pay-as-you-go -- 0.5% per transaction on mainnet. No monthly minimum.", "Pay-as-you-go")
    pdf.bullet("Pro -- $49/month. Unlimited transactions, 0% fees, priority support.", "Pro")
    pdf.bullet("Enterprise -- $2,500 one-time. Full source code, policy engine, admin dashboard, Copilot Studio plugins.", "Enterprise")

    pdf.section_heading("Technology Stack")
    pdf.body_text(
        "Backend: ASP.NET Core 8.0 on Azure/Render. Database: PostgreSQL. "
        "Blockchain: Circle Developer Controlled Wallets + USDC on Base, Ethereum, and Arc networks. "
        "AI: Microsoft Copilot Studio, Semantic Kernel, Agent Framework. "
        "Protocol: x402 V2 (EIP-3009 transferWithAuthorization)."
    )

    pdf.info_box(
        "Get Started",
        "Sandbox: sandbox.agentrails.io/swagger  |  Docs: www.agentrails.io/docs  |  "
        "Enterprise: sales@agentrails.io  |  GitHub: github.com/kmatthewsio/AgenticCommerce"
    )

    path = os.path.join(OUT, "AgentRails-Platform-Overview.pdf")
    pdf.output(path)
    print(f"  Created AgentRails-Platform-Overview.pdf")


# =============================================================================
# PDF 2: Enterprise Architecture Guide
# =============================================================================

def generate_architecture_guide():
    pdf = BrandedPDF("Enterprise Architecture Guide")
    pdf.cover_page(
        "Enterprise Architecture Guide",
        "Copilot Studio, MCP Servers, Power Platform, and x402 Payments",
    )

    # Page 2
    pdf.add_page()

    pdf.section_heading("Architecture Overview")
    pdf.body_text(
        "AgentRails enterprise deployments integrate across the full Microsoft 365 stack. "
        "The architecture connects Copilot Studio agents to internal systems via MCP (Model Context Protocol) "
        "servers, orchestrates workflows through Power Automate, stores governance data in Dataverse, "
        "and enables autonomous payments via the x402 protocol."
    )

    pdf.sub_heading("Core Components")
    pdf.bullet("Copilot Studio -- Build and deploy custom AI agents with governed capabilities in Microsoft Teams", "Copilot Studio")
    pdf.bullet("MCP Servers -- Model Context Protocol servers provide scoped, secure access to internal data and APIs", "MCP Servers")
    pdf.bullet("Power Automate -- Orchestrate approval workflows, escalation paths, and compliance reporting", "Power Automate")
    pdf.bullet("Dataverse -- Central store for agent state, policies, audit logs, and security roles", "Dataverse")
    pdf.bullet("x402 Payment Layer -- Autonomous USDC payments with enterprise spending controls", "x402 Payment Layer")
    pdf.bullet("Admin Dashboard -- Next.js management console for agents, transactions, policies, and audit logs", "Admin Dashboard")

    pdf.section_heading("Copilot Studio Integration")
    pdf.body_text(
        "AgentRails provides two pre-built Copilot Studio action sets that can be imported directly:"
    )
    pdf.sub_heading("FinanceOps Actions")
    pdf.body_text(
        "Revenue queries, transaction search, payment analytics, and spending reports. "
        "Finance teams ask natural-language questions in Teams: \"What's our x402 revenue this month?\" "
        "The copilot calls the AgentRails API and returns formatted results with charts."
    )
    pdf.sub_heading("Agent Executor Actions")
    pdf.body_text(
        "Agent management, policy enforcement, kill switches, and status monitoring. "
        "Operators manage agents conversationally: \"Pause research-agent-01\" or "
        "\"Show agents over their spending limit.\""
    )

    pdf.section_heading("MCP Server Architecture")
    pdf.body_text(
        "Model Context Protocol servers act as secure bridges between Copilot Studio agents "
        "and your internal systems. Each MCP server exposes a scoped set of tools that agents "
        "can invoke, with authentication, rate limiting, and audit logging built in."
    )
    pdf.bullet("Data access -- Query internal databases, CRMs, ERPs without exposing connection strings", "Data access")
    pdf.bullet("API orchestration -- Compose multi-step API calls into single agent-friendly tools", "API orchestration")
    pdf.bullet("File operations -- Read/write documents in SharePoint, OneDrive with proper permissions", "File operations")
    pdf.bullet("Custom logic -- Business rules, calculations, validations specific to your domain", "Custom logic")

    pdf.section_heading("Governance Framework")
    pdf.body_text("AgentRails enforces governance at every layer:")

    pdf.sub_heading("Spending Policies")
    pdf.bullet("Per-agent budget limits (daily, weekly, monthly)")
    pdf.bullet("Per-transaction caps")
    pdf.bullet("Destination address whitelists")
    pdf.bullet("Rate controls (max transactions per minute/hour)")

    pdf.sub_heading("Security Roles (Dataverse)")
    pdf.bullet("System Admin -- Full access to all agent and payment operations", "System Admin")
    pdf.bullet("Finance Manager -- Revenue reports, spending policies, transaction queries", "Finance Manager")
    pdf.bullet("Agent Operator -- Agent lifecycle management, status monitoring, kill switches", "Agent Operator")
    pdf.bullet("Auditor -- Read-only access to audit logs, transaction history, policy changes", "Auditor")
    pdf.bullet("Read-Only Viewer -- Dashboard viewing only", "Read-Only Viewer")

    pdf.sub_heading("Audit Trail")
    pdf.body_text(
        "Every agent action, payment, and policy change is logged with timestamps, user identity, "
        "and on-chain transaction hashes. Logs are stored in Dataverse and can be surfaced via "
        "Power BI dashboards or queried through the Copilot Studio FinanceOps actions."
    )

    pdf.section_heading("Deployment Options")

    pdf.sub_heading("Hosted (Pro Tier)")
    pdf.body_text(
        "AgentRails runs the x402 facilitator, API server, and admin dashboard. "
        "You connect your Copilot Studio agents via the OpenAPI connector. "
        "Best for teams that want fast time-to-value without infrastructure management."
    )

    pdf.sub_heading("Self-Hosted (Enterprise Tier)")
    pdf.body_text(
        "Full source code deployed on your own infrastructure. ASP.NET Core 8.0 API "
        "with PostgreSQL, deployed to Azure App Service, Azure Container Apps, or any Docker host. "
        "Includes policy engine, admin dashboard, and all Copilot Studio integrations. "
        "Best for regulated industries or organizations requiring full data sovereignty."
    )

    pdf.section_heading("Open Source Repositories")
    pdf.bullet("agentrails-powerplatform-demo -- Custom connector (24 ops), Power Automate flows, solution package")
    pdf.bullet("agentrails-copilot-actions -- FinanceOps + Agent Executor Copilot Studio actions")
    pdf.bullet("agentrails-dataverse-integration -- Table definitions, sync flows, security roles")

    pdf.info_box(
        "Book an Architecture Review",
        "We offer 2-week assessment engagements to audit your M365 environment, "
        "map agent use cases, and design the governance framework. Contact sales@agentrails.io"
    )

    path = os.path.join(OUT, "AgentRails-Enterprise-Architecture-Guide.pdf")
    pdf.output(path)
    print(f"  Created AgentRails-Enterprise-Architecture-Guide.pdf")


# =============================================================================
# PDF 3: x402 Protocol Whitepaper
# =============================================================================

def generate_protocol_whitepaper():
    pdf = BrandedPDF("x402 Protocol Whitepaper")
    pdf.cover_page(
        "x402 Protocol Whitepaper",
        "HTTP-Native Payments for the Agent Economy",
    )

    # Page 2
    pdf.add_page()

    pdf.section_heading("Abstract")
    pdf.body_text(
        "The x402 protocol enables HTTP-native payments for AI agents and automated systems. "
        "By leveraging HTTP status code 402 (Payment Required), the protocol allows any API "
        "to request payment inline with the HTTP request/response cycle. Agents pay per-request "
        "in USDC stablecoin using EIP-3009 (transferWithAuthorization), eliminating the need for "
        "API keys, subscription tiers, and manual credential management."
    )
    pdf.body_text(
        "This whitepaper describes the x402 V2 protocol specification as implemented by AgentRails, "
        "including the payment flow, security model, network support, and enterprise governance extensions."
    )

    pdf.section_heading("The Problem")
    pdf.body_text(
        "Today's API economy relies on a stack of legacy abstractions: developer portals, API keys, "
        "OAuth tokens, subscription tiers, and monthly invoices. This model was designed for human "
        "developers, not autonomous agents."
    )
    pdf.body_text("For AI agents, this model breaks down:")
    pdf.bullet("Agents cannot sign up for accounts or complete email verification")
    pdf.bullet("API keys are static secrets that must be provisioned, stored, and rotated")
    pdf.bullet("Subscription tiers force pre-commitment to usage levels")
    pdf.bullet("Each new vendor requires human approval and procurement")
    pdf.bullet("Invoice reconciliation across dozens of services is operationally expensive")
    pdf.body_text(
        "The result: agents are bottlenecked by human gatekeeping at every API boundary. "
        "The x402 protocol removes this bottleneck by making payment the authentication mechanism."
    )

    pdf.section_heading("Protocol Design")

    pdf.sub_heading("Core Principle")
    pdf.body_text(
        "x402 uses HTTP status code 402 (Payment Required) as defined in RFC 7231. "
        "The protocol is stateless, requires no pre-registration, and works with any HTTP client "
        "that can read response headers and retry requests."
    )

    pdf.sub_heading("Payment Flow")
    pdf.body_text("1. CLIENT sends a standard HTTP request to a protected endpoint.")
    pdf.body_text(
        "2. SERVER returns HTTP 402 with a PAYMENT-REQUIRED header containing a base64-encoded "
        "JSON payload specifying: protocol version, price (in smallest units), currency, "
        "network (CAIP-2 format), receiver address, and payment description."
    )
    pdf.body_text(
        "3. CLIENT inspects the payment requirements, checks budget limits, and signs an "
        "EIP-3009 transferWithAuthorization message using the agent's wallet private key."
    )
    pdf.body_text(
        "4. CLIENT retries the original request with a PAYMENT-SIGNATURE header containing "
        "the signed payment payload (from, to, value, validAfter, validBefore, nonce, signature)."
    )
    pdf.body_text(
        "5. SERVER (or facilitator) verifies the signature off-chain using EIP-712 typed data "
        "hashing and ECDSA signature recovery. If valid, the facilitator submits the "
        "transferWithAuthorization transaction on-chain."
    )
    pdf.body_text(
        "6. SERVER returns the requested data along with a PAYMENT-RESPONSE header containing "
        "the transaction hash and settlement status."
    )

    pdf.section_heading("Security Model")

    pdf.sub_heading("EIP-3009: transferWithAuthorization")
    pdf.body_text(
        "x402 uses EIP-3009, a USDC-native standard that allows gasless, authorized transfers. "
        "The payer signs a typed data message (EIP-712) authorizing a specific transfer. The "
        "facilitator submits the transaction, paying gas fees on behalf of the agent. This means:"
    )
    pdf.bullet("Agents never need native gas tokens (ETH, etc.)")
    pdf.bullet("Each authorization is single-use (unique nonce)")
    pdf.bullet("Authorizations have time bounds (validAfter, validBefore)")
    pdf.bullet("The payer's private key never leaves the agent's environment")

    pdf.sub_heading("Off-Chain Verification")
    pdf.body_text(
        "Before submitting on-chain, the facilitator verifies the signature off-chain using "
        "EIP-712 typed data hashing and ecrecover. This prevents invalid transactions from "
        "consuming gas and enables sub-second verification."
    )

    pdf.sub_heading("Facilitator Role")
    pdf.body_text(
        "The facilitator is a trusted intermediary that verifies signatures and submits "
        "on-chain transactions. In hosted mode, AgentRails operates the facilitator. In "
        "enterprise mode, organizations run their own facilitator with full control over "
        "settlement timing, batching, and network selection."
    )

    pdf.section_heading("Network Support")
    pdf.body_text("x402 V2 supports multiple EVM-compatible networks using CAIP-2 identifiers:")
    pdf.bullet("Base (eip155:8453) -- Coinbase L2, low gas fees, fast finality", "Base (eip155:8453)")
    pdf.bullet("Base Sepolia (eip155:84532) -- Base testnet for development", "Base Sepolia (eip155:84532)")
    pdf.bullet("Ethereum (eip155:1) -- Ethereum mainnet for high-value transactions", "Ethereum (eip155:1)")
    pdf.bullet("Ethereum Sepolia (eip155:11155111) -- Ethereum testnet", "Ethereum Sepolia (eip155:11155111)")
    pdf.bullet("Arc (eip155:5042002) -- Circle's L2 with native USDC (testnet)", "Arc (eip155:5042002)")

    pdf.body_text(
        "Multi-network support allows servers to accept payment on any supported chain, "
        "and agents can pay on the network with the lowest fees or fastest settlement."
    )

    pdf.section_heading("Enterprise Extensions")
    pdf.body_text(
        "AgentRails extends the base x402 protocol with enterprise governance features:"
    )
    pdf.bullet("Policy engine -- Server-side enforcement of spending limits, rate controls, and destination rules", "Policy engine")
    pdf.bullet("Audit logging -- Every payment decision logged with timestamps, agent identity, and on-chain hashes", "Audit logging")
    pdf.bullet("Kill switches -- Instantly revoke an agent's payment capability", "Kill switches")
    pdf.bullet("Approval workflows -- Route high-value transactions through human approval via Power Automate", "Approval workflows")
    pdf.bullet("Trust scoring -- Verify service reputation before authorizing payment", "Trust scoring")

    pdf.section_heading("SDK Integrations")
    pdf.body_text(
        "AgentRails provides SDK packages that handle the full x402 flow automatically. "
        "When an agent's HTTP request receives a 402 response, the SDK:"
    )
    pdf.bullet("Parses the PAYMENT-REQUIRED header")
    pdf.bullet("Checks the agent's budget and policy limits")
    pdf.bullet("Signs the EIP-3009 authorization")
    pdf.bullet("Retries the request with the PAYMENT-SIGNATURE header")
    pdf.bullet("Returns the data to the agent transparently")

    pdf.body_text("Available SDKs:")
    pdf.bullet("langchain-x402 (PyPI) -- LangChain toolkit with x402 payment tools")
    pdf.bullet("crewai-x402 (PyPI) -- CrewAI toolkit for multi-agent payment crews")
    pdf.bullet("AgentRails.SemanticKernel.X402 (NuGet) -- Semantic Kernel plugin")
    pdf.bullet("AgentRails.AgentFramework.X402 (NuGet) -- Microsoft Agent Framework tools")
    pdf.bullet("Copilot Studio connector -- OpenAPI spec import for Power Platform")

    pdf.section_heading("Comparison: x402 vs Traditional API Access")
    pdf.body_text(
        "Traditional: Sign up, verify email, add credit card, generate API key, store securely, "
        "rotate periodically, pay monthly subscription regardless of usage, reconcile invoices."
    )
    pdf.body_text(
        "x402: Agent calls API, gets 402, pays exact amount per-request, gets instant access. "
        "No signup, no credentials, no subscriptions. Every payment is cryptographically verifiable."
    )

    pdf.info_box(
        "Learn More",
        "Protocol spec: github.com/kmatthewsio/AgenticCommerce  |  "
        "Docs: www.agentrails.io/docs  |  Sandbox: sandbox.agentrails.io/swagger"
    )

    path = os.path.join(OUT, "AgentRails-x402-Protocol-Whitepaper.pdf")
    pdf.output(path)
    print(f"  Created AgentRails-x402-Protocol-Whitepaper.pdf")


# =============================================================================
# MAIN
# =============================================================================

if __name__ == "__main__":
    print("Generating Microsoft Marketplace PDFs...\n")
    generate_platform_overview()
    generate_architecture_guide()
    generate_protocol_whitepaper()
    print(f"\nAll PDFs saved to: {OUT}")
