"""
Generate Microsoft Marketplace visual assets for AgentRails.
- Logo PNGs: 300x300, 90x90, 48x48
- Screenshots: 1280x720 (5 screenshots)
- Video thumbnail: 1280x720
"""

from PIL import Image, ImageDraw, ImageFont
import math
import os

OUT = os.path.dirname(os.path.abspath(__file__))

# Brand colors
INDIGO_500 = (99, 102, 241)
INDIGO_600 = (79, 70, 229)
INDIGO_700 = (67, 56, 202)
INDIGO_800 = (55, 48, 163)
INDIGO_900 = (49, 46, 129)
CYAN_400 = (34, 211, 238)
CYAN_500 = (6, 182, 212)
WHITE = (255, 255, 255)
GRAY_50 = (250, 250, 250)
GRAY_100 = (244, 244, 245)
GRAY_200 = (228, 228, 231)
GRAY_300 = (212, 212, 216)
GRAY_400 = (161, 161, 170)
GRAY_500 = (113, 113, 122)
GRAY_600 = (82, 82, 91)
GRAY_700 = (63, 63, 70)
GRAY_800 = (39, 39, 42)
GRAY_900 = (24, 24, 27)
EMERALD_500 = (16, 185, 129)
RED_400 = (248, 113, 113)
AMBER_400 = (251, 191, 36)
GREEN_400 = (74, 222, 128)
VIOLET_500 = (139, 92, 246)
BLUE_500 = (59, 130, 246)
TEAL_500 = (20, 184, 166)


def gradient_fill(draw, rect, color1, color2, direction="diagonal"):
    """Fill a rectangle with a gradient."""
    x1, y1, x2, y2 = rect
    w = x2 - x1
    h = y2 - y1
    for i in range(h):
        for j in range(w):
            if direction == "diagonal":
                t = (i / h + j / w) / 2
            elif direction == "horizontal":
                t = j / w
            else:
                t = i / h
            r = int(color1[0] + (color2[0] - color1[0]) * t)
            g = int(color1[1] + (color2[1] - color1[1]) * t)
            b = int(color1[2] + (color2[2] - color1[2]) * t)
            draw.point((x1 + j, y1 + i), fill=(r, g, b))


def gradient_image(size, color1, color2, direction="diagonal"):
    """Create a full gradient image."""
    img = Image.new("RGB", size)
    draw = ImageDraw.Draw(img)
    gradient_fill(draw, (0, 0, size[0], size[1]), color1, color2, direction)
    return img


def rounded_rect(draw, rect, radius, fill):
    """Draw a rounded rectangle."""
    x1, y1, x2, y2 = rect
    draw.rounded_rectangle(rect, radius=radius, fill=fill)


def get_font(size, bold=False):
    """Try to get a nice font, fall back to default."""
    font_names = [
        "C:/Windows/Fonts/segoeui.ttf",
        "C:/Windows/Fonts/segoeuib.ttf",
        "C:/Windows/Fonts/arial.ttf",
        "C:/Windows/Fonts/arialbd.ttf",
    ]
    if bold:
        font_names = [
            "C:/Windows/Fonts/segoeuib.ttf",
            "C:/Windows/Fonts/arialbd.ttf",
        ] + font_names
    for name in font_names:
        try:
            return ImageFont.truetype(name, size)
        except (OSError, IOError):
            continue
    return ImageFont.load_default()


def get_mono_font(size):
    """Get a monospace font."""
    mono_fonts = [
        "C:/Windows/Fonts/consola.ttf",
        "C:/Windows/Fonts/cour.ttf",
    ]
    for name in mono_fonts:
        try:
            return ImageFont.truetype(name, size)
        except (OSError, IOError):
            continue
    return ImageFont.load_default()


# =============================================================================
# LOGO GENERATION
# =============================================================================

def draw_terminal_icon(draw, cx, cy, size, color=WHITE):
    """Draw a terminal/code icon (>_ prompt)."""
    s = size
    # > chevron
    points_chevron = [
        (cx - s * 0.35, cy - s * 0.25),
        (cx - s * 0.05, cy),
        (cx - s * 0.35, cy + s * 0.25),
    ]
    draw.line(points_chevron, fill=color, width=max(2, int(s * 0.08)))

    # _ underscore
    draw.line(
        [(cx + s * 0.05, cy + s * 0.25), (cx + s * 0.35, cy + s * 0.25)],
        fill=color,
        width=max(2, int(s * 0.08)),
    )


def generate_logo(size, filename):
    """Generate a square logo with gradient background and terminal icon."""
    img = gradient_image((size, size), INDIGO_500, CYAN_500, "diagonal")
    draw = ImageDraw.Draw(img)

    # Round corners by masking
    mask = Image.new("L", (size, size), 0)
    mask_draw = ImageDraw.Draw(mask)
    radius = int(size * 0.18)
    mask_draw.rounded_rectangle([(0, 0), (size, size)], radius=radius, fill=255)

    # Apply rounded corners
    bg = Image.new("RGB", (size, size), (255, 255, 255))
    bg.paste(img, mask=mask)
    img = bg

    draw = ImageDraw.Draw(img)
    # Draw terminal icon centered
    draw_terminal_icon(draw, size // 2, size // 2, size * 0.55, WHITE)

    img.save(os.path.join(OUT, filename))
    print(f"  Created {filename} ({size}x{size})")


# =============================================================================
# SCREENSHOT GENERATION
# =============================================================================

def draw_browser_chrome(draw, w, h):
    """Draw browser-like chrome at the top."""
    # Title bar
    draw.rectangle([(0, 0), (w, 44)], fill=GRAY_100)
    draw.line([(0, 44), (w, 44)], fill=GRAY_200, width=1)

    # Window dots
    for i, color in enumerate([RED_400, AMBER_400, GREEN_400]):
        draw.ellipse(
            [(16 + i * 22, 15), (28 + i * 22, 27)],
            fill=color,
        )

    # URL bar
    draw.rounded_rectangle(
        [(120, 10), (w - 120, 34)],
        radius=6,
        fill=WHITE,
        outline=GRAY_200,
    )


def draw_nav_bar(draw, w, y_start):
    """Draw the AgentRails nav bar."""
    draw.rectangle([(0, y_start), (w, y_start + 56)], fill=(255, 255, 255, 230))
    draw.line([(0, y_start + 56), (w, y_start + 56)], fill=GRAY_100, width=1)

    # Logo square
    draw.rounded_rectangle(
        [(24, y_start + 12), (56, y_start + 44)],
        radius=6,
        fill=INDIGO_500,
    )
    # Small terminal icon in logo
    draw_terminal_icon(draw, 40, y_start + 28, 16, WHITE)

    # Brand name
    font_brand = get_font(16, bold=True)
    draw.text((64, y_start + 17), "AgentRails", fill=GRAY_900, font=font_brand)

    # Nav links
    font_nav = get_font(13)
    links = ["Features", "Docs", "Blog", "Enterprise", "Pricing"]
    x = w - 500
    for link in links:
        draw.text((x, y_start + 20), link, fill=GRAY_600, font=font_nav)
        x += 80

    # CTA button
    draw.rounded_rectangle(
        [(w - 140, y_start + 12), (w - 24, y_start + 44)],
        radius=8,
        fill=INDIGO_600,
    )
    font_btn = get_font(12, bold=True)
    draw.text((w - 128, y_start + 19), "Try Sandbox", fill=WHITE, font=font_btn)


def screenshot_1_swagger(w=1280, h=720):
    """Screenshot 1: Sandbox API Explorer — Swagger UI."""
    img = Image.new("RGB", (w, h), GRAY_50)
    draw = ImageDraw.Draw(img)

    draw_browser_chrome(draw, w, h)

    # URL
    font_url = get_font(11)
    draw.text((140, 15), "sandbox.agentrails.io/swagger", fill=GRAY_600, font=font_url)

    # Swagger header
    draw.rectangle([(0, 45), (w, 105)], fill=INDIGO_900)
    font_h = get_font(22, bold=True)
    draw.text((30, 58), "AgentRails API — Sandbox", fill=WHITE, font=font_h)
    font_sub = get_font(13)
    draw.text((30, 85), "x402 Payment Protocol  |  v2.0  |  Base Sepolia Testnet", fill=(180, 180, 220), font=font_sub)

    # Sidebar + content area
    draw.rectangle([(0, 106), (260, h)], fill=WHITE)
    draw.line([(260, 106), (260, h)], fill=GRAY_200, width=1)

    # Sidebar items
    font_side = get_font(13, bold=True)
    font_side_item = get_font(12)
    sections = [
        ("x402 Payments", ["GET /x402/protected/analysis", "GET /x402/protected/data", "GET /x402/pricing"]),
        ("Facilitator", ["POST /facilitator/verify", "POST /facilitator/settle"]),
        ("Analytics", ["GET /x402/payments", "GET /x402/stats"]),
        ("Agents", ["GET /agents", "POST /agents"]),
    ]
    y = 120
    for section, items in sections:
        draw.text((16, y), section, fill=INDIGO_600, font=font_side)
        y += 24
        for item in items:
            draw.text((24, y), item, fill=GRAY_600, font=font_side_item)
            y += 22
        y += 12

    # Main content — expanded endpoint
    x_main = 280
    y_main = 120

    # Endpoint card - GET /x402/protected/analysis
    draw.rounded_rectangle(
        [(x_main, y_main), (w - 30, y_main + 42)],
        radius=4,
        fill=(235, 255, 235),
        outline=EMERALD_500,
    )
    font_method = get_font(13, bold=True)
    draw.text((x_main + 12, y_main + 12), "GET", fill=EMERALD_500, font=font_method)
    font_path = get_font(13)
    draw.text((x_main + 50, y_main + 12), "/api/x402/protected/analysis", fill=GRAY_700, font=font_path)
    draw.text((x_main + 400, y_main + 12), "$0.01 USDC — Premium market analysis", fill=GRAY_500, font=font_path)

    y_main += 56

    # Response section
    draw.text((x_main, y_main), "Response (402 Payment Required)", fill=GRAY_700, font=get_font(14, bold=True))
    y_main += 28

    # Code block
    code_h = 280
    draw.rounded_rectangle(
        [(x_main, y_main), (w - 30, y_main + code_h)],
        radius=8,
        fill=INDIGO_900,
    )

    mono = get_mono_font(12)
    code_lines = [
        ('  {', GRAY_300),
        ('    "status": 402,', GRAY_300),
        ('    "message": "Payment Required",', GRAY_300),
        ('    "x402": {', CYAN_400),
        ('      "version": "v2",', GRAY_300),
        ('      "price": "10000",', AMBER_400),
        ('      "currency": "USDC",', GRAY_300),
        ('      "network": "eip155:84532",', GRAY_300),
        ('      "receiver": "0x3f...a8c2",', GRAY_300),
        ('      "description": "Premium analysis"', EMERALD_500),
        ('    }', CYAN_400),
        ('  }', GRAY_300),
    ]

    y_code = y_main + 16
    for line, color in code_lines:
        draw.text((x_main + 16, y_code), line, fill=color, font=mono)
        y_code += 20

    # Status badge
    y_main += code_h + 16
    draw.rounded_rectangle(
        [(x_main, y_main), (x_main + 200, y_main + 30)],
        radius=4,
        fill=(255, 243, 232),
    )
    draw.text((x_main + 8, y_main + 7), "Status: 402 Payment Required", fill=(194, 120, 3), font=get_font(11, bold=True))

    # Second endpoint - collapsed
    y_main += 48
    draw.rounded_rectangle(
        [(x_main, y_main), (w - 30, y_main + 42)],
        radius=4,
        fill=(235, 255, 235),
        outline=(200, 230, 200),
    )
    draw.text((x_main + 12, y_main + 12), "GET", fill=EMERALD_500, font=font_method)
    draw.text((x_main + 50, y_main + 12), "/api/x402/protected/data", fill=GRAY_700, font=font_path)
    draw.text((x_main + 400, y_main + 12), "$0.001 USDC — Market data endpoint", fill=GRAY_500, font=font_path)

    # Third endpoint
    y_main += 52
    draw.rounded_rectangle(
        [(x_main, y_main), (w - 30, y_main + 42)],
        radius=4,
        fill=(230, 240, 255),
        outline=(180, 200, 240),
    )
    draw.text((x_main + 12, y_main + 12), "GET", fill=BLUE_500, font=font_method)
    draw.text((x_main + 50, y_main + 12), "/api/x402/pricing", fill=GRAY_700, font=font_path)
    draw.text((x_main + 400, y_main + 12), "No auth — Pricing information", fill=GRAY_500, font=font_path)

    img.save(os.path.join(OUT, "screenshot-1-swagger.png"))
    print("  Created screenshot-1-swagger.png")


def screenshot_2_sdk(w=1280, h=720):
    """Screenshot 2: SDK Integration — code snippet."""
    img = gradient_image((w, h), INDIGO_900, (20, 20, 40), "vertical")
    draw = ImageDraw.Draw(img)

    # Title
    font_title = get_font(36, bold=True)
    draw.text((60, 40), "Add x402 payments in 3 lines", fill=WHITE, font=font_title)
    font_sub = get_font(18)
    draw.text((60, 88), "SDKs for LangChain, CrewAI, Semantic Kernel, Agent Framework, and Copilot Studio", fill=GRAY_400, font=font_sub)

    # Code window
    code_y = 140
    code_x = 60
    code_w = w - 120
    code_h = 340
    draw.rounded_rectangle(
        [(code_x, code_y), (code_x + code_w, code_y + code_h)],
        radius=12,
        fill=(15, 15, 30),
    )

    # Window bar
    draw.rounded_rectangle(
        [(code_x, code_y), (code_x + code_w, code_y + 36)],
        radius=12,
        fill=(30, 30, 50),
    )
    draw.rectangle([(code_x, code_y + 24), (code_x + code_w, code_y + 36)], fill=(30, 30, 50))

    # Dots
    for i, color in enumerate([RED_400, AMBER_400, GREEN_400]):
        draw.ellipse(
            [(code_x + 14 + i * 20, code_y + 10), (code_x + 26 + i * 20, code_y + 22)],
            fill=color,
        )
    draw.text((code_x + 90, code_y + 9), "agent_with_payments.py", fill=GRAY_500, font=get_font(12))

    # Code content
    mono = get_mono_font(15)
    lines = [
        [("from", VIOLET_500), (" langchain_x402 ", WHITE), ("import", VIOLET_500), (" X402Toolkit", CYAN_400)],
        [("from", VIOLET_500), (" langchain_openai ", WHITE), ("import", VIOLET_500), (" ChatOpenAI", CYAN_400)],
        [("from", VIOLET_500), (" langchain.agents ", WHITE), ("import", VIOLET_500), (" create_tool_calling_agent", CYAN_400)],
        [],
        [("# Create an agent with x402 payment capabilities", GRAY_500)],
        [("toolkit = X402Toolkit(", WHITE), ("wallet_private_key", AMBER_400), ("=", WHITE), ("key", EMERALD_500), (")", WHITE)],
        [("llm = ChatOpenAI(", WHITE), ("model", AMBER_400), ("=", WHITE), ('"gpt-4o"', EMERALD_500), (")", WHITE)],
        [("agent = create_tool_calling_agent(", WHITE), ("llm", CYAN_400), (", ", WHITE), ("toolkit.get_tools()", CYAN_400), (")", WHITE)],
        [],
        [("# Agent autonomously pays for APIs it calls", GRAY_500)],
        [("result = agent.invoke(", WHITE), ('"Get premium market analysis"', EMERALD_500), (")", WHITE)],
        [],
        [("# Payment settled on-chain, data returned instantly", GRAY_500)],
        [("print(result.cost)    ", WHITE), ("# $0.01 USDC", GRAY_500)],
        [("print(result.tx_hash) ", WHITE), ("# 0xabc...def", GRAY_500)],
    ]

    y = code_y + 50
    for line_parts in lines:
        x = code_x + 24
        for text, color in line_parts:
            draw.text((x, y), text, fill=color, font=mono)
            bbox = draw.textbbox((0, 0), text, font=mono)
            x += bbox[2] - bbox[0]
        y += 22

    # SDK badges at bottom
    badge_y = code_y + code_h + 40
    sdks = [
        ("LangChain", INDIGO_500, CYAN_500),
        ("CrewAI", AMBER_400, (255, 160, 50)),
        ("Semantic Kernel", VIOLET_500, (180, 100, 255)),
        ("Agent Framework", BLUE_500, INDIGO_600),
        ("Copilot Studio", TEAL_500, EMERALD_500),
    ]
    badge_x = 60
    for name, c1, c2 in sdks:
        tw = len(name) * 11 + 32
        draw.rounded_rectangle(
            [(badge_x, badge_y), (badge_x + tw, badge_y + 40)],
            radius=8,
            fill=c1,
        )
        draw.text((badge_x + 16, badge_y + 10), name, fill=WHITE, font=get_font(14, bold=True))
        badge_x += tw + 16

    # Install commands
    cmd_y = badge_y + 56
    mono_sm = get_mono_font(13)
    cmds = [
        "pip install langchain-x402",
        "pip install crewai-x402",
        "dotnet add package AgentRails.SemanticKernel.X402",
        "dotnet add package AgentRails.AgentFramework.X402",
    ]
    for cmd in cmds:
        draw.text((76, cmd_y), "$ " + cmd, fill=GRAY_400, font=mono_sm)
        cmd_y += 22

    img.save(os.path.join(OUT, "screenshot-2-sdk.png"))
    print("  Created screenshot-2-sdk.png")


def screenshot_3_dashboard(w=1280, h=720):
    """Screenshot 3: Enterprise Dashboard."""
    img = Image.new("RGB", (w, h), GRAY_50)
    draw = ImageDraw.Draw(img)

    # Sidebar
    draw.rectangle([(0, 0), (220, h)], fill=INDIGO_900)

    # Sidebar logo
    draw.rounded_rectangle([(20, 20), (48, 48)], radius=6, fill=INDIGO_500)
    draw_terminal_icon(draw, 34, 34, 14, WHITE)
    draw.text((56, 26), "AgentRails", fill=WHITE, font=get_font(16, bold=True))

    # Sidebar menu
    menu_items = [
        ("Dashboard", True),
        ("Agents", False),
        ("Transactions", False),
        ("Policies", False),
        ("Audit Log", False),
        ("Settings", False),
    ]
    y_menu = 80
    for label, active in menu_items:
        if active:
            draw.rounded_rectangle(
                [(12, y_menu), (208, y_menu + 38)],
                radius=6,
                fill=(79, 70, 229),
            )
        draw.text((24, y_menu + 10), label, fill=WHITE if active else (180, 180, 220), font=get_font(13))
        y_menu += 46

    # Main content area
    x_main = 240
    y_main = 20

    # Header
    draw.text((x_main, y_main), "Dashboard", fill=GRAY_900, font=get_font(28, bold=True))
    draw.text((x_main, y_main + 36), "Agent payment overview — Last 30 days", fill=GRAY_500, font=get_font(13))

    # Stat cards row
    y_cards = y_main + 72
    card_w = (w - x_main - 60) // 4
    stats = [
        ("Total Payments", "$12,847.32", "+23.5%", EMERALD_500),
        ("Active Agents", "47", "+8", BLUE_500),
        ("Avg per Transaction", "$0.0043", "-12.1%", AMBER_400),
        ("Policy Violations", "3", "Last 30d", RED_400),
    ]
    for i, (label, value, change, color) in enumerate(stats):
        cx = x_main + i * (card_w + 15)
        draw.rounded_rectangle(
            [(cx, y_cards), (cx + card_w, y_cards + 95)],
            radius=10,
            fill=WHITE,
            outline=GRAY_200,
        )
        draw.text((cx + 16, y_cards + 12), label, fill=GRAY_500, font=get_font(11))
        draw.text((cx + 16, y_cards + 32), value, fill=GRAY_900, font=get_font(22, bold=True))
        draw.text((cx + 16, y_cards + 65), change, fill=color, font=get_font(11, bold=True))

    # Transaction table
    y_table = y_cards + 120
    draw.rounded_rectangle(
        [(x_main, y_table), (w - 20, h - 20)],
        radius=10,
        fill=WHITE,
        outline=GRAY_200,
    )
    draw.text((x_main + 20, y_table + 16), "Recent Transactions", fill=GRAY_900, font=get_font(16, bold=True))

    # Table header
    y_th = y_table + 50
    headers = ["Agent", "Endpoint", "Amount", "Network", "Status", "Time"]
    col_widths = [140, 260, 100, 120, 100, 160]
    x_col = x_main + 20
    for header, cw in zip(headers, col_widths):
        draw.text((x_col, y_th), header, fill=GRAY_500, font=get_font(11, bold=True))
        x_col += cw
    draw.line([(x_main + 20, y_th + 22), (w - 40, y_th + 22)], fill=GRAY_200, width=1)

    # Table rows
    rows = [
        ("research-agent-01", "/api/market/analysis", "$0.01", "Base Sepolia", "Settled", "2 min ago"),
        ("finance-copilot", "/api/x402/stats", "$0.001", "Base Sepolia", "Settled", "5 min ago"),
        ("data-agent-03", "/api/premium/data", "$0.05", "Arc Testnet", "Settled", "12 min ago"),
        ("sales-agent-07", "/api/leads/enrich", "$0.02", "Base Sepolia", "Pending", "15 min ago"),
        ("ops-agent-12", "/api/compute/gpu", "$0.10", "Ethereum", "Settled", "23 min ago"),
        ("research-agent-01", "/api/news/summary", "$0.005", "Base Sepolia", "Settled", "31 min ago"),
        ("qa-agent-04", "/api/test/validate", "$0.001", "Arc Testnet", "Failed", "45 min ago"),
    ]

    y_row = y_th + 30
    for agent, endpoint, amount, network, status, time in rows:
        x_col = x_main + 20
        font_row = get_font(12)

        draw.text((x_col, y_row), agent, fill=INDIGO_600, font=get_font(12, bold=True))
        x_col += col_widths[0]
        draw.text((x_col, y_row), endpoint, fill=GRAY_700, font=font_row)
        x_col += col_widths[1]
        draw.text((x_col, y_row), amount, fill=GRAY_900, font=get_font(12, bold=True))
        x_col += col_widths[2]
        draw.text((x_col, y_row), network, fill=GRAY_600, font=font_row)
        x_col += col_widths[3]

        # Status badge
        status_colors = {"Settled": EMERALD_500, "Pending": AMBER_400, "Failed": RED_400}
        sc = status_colors.get(status, GRAY_400)
        draw.rounded_rectangle(
            [(x_col, y_row - 2), (x_col + 65, y_row + 16)],
            radius=4,
            fill=(*sc, 30),
        )
        draw.text((x_col + 8, y_row), status, fill=sc, font=get_font(11))
        x_col += col_widths[4]

        draw.text((x_col, y_row), time, fill=GRAY_500, font=font_row)

        y_row += 32
        if y_row > h - 50:
            break

    img.save(os.path.join(OUT, "screenshot-3-dashboard.png"))
    print("  Created screenshot-3-dashboard.png")


def screenshot_4_copilot(w=1280, h=720):
    """Screenshot 4: Copilot Studio in Teams — finance team querying."""
    img = Image.new("RGB", (w, h), (240, 240, 245))
    draw = ImageDraw.Draw(img)

    # Teams-like header
    draw.rectangle([(0, 0), (w, 48)], fill=(75, 60, 165))
    draw.text((20, 14), "Microsoft Teams", fill=WHITE, font=get_font(16, bold=True))
    draw.text((w - 200, 16), "AgentRails Finance", fill=(200, 200, 240), font=get_font(13))

    # Left sidebar (Teams channel list)
    draw.rectangle([(0, 48), (260, h)], fill=WHITE)
    draw.line([(260, 48), (260, h)], fill=GRAY_200, width=1)

    channels = ["General", "AgentRails Finance", "Agent Ops", "Governance Alerts"]
    y_ch = 68
    for i, ch in enumerate(channels):
        if i == 1:
            draw.rounded_rectangle(
                [(8, y_ch - 4), (252, y_ch + 28)],
                radius=6,
                fill=(238, 242, 255),
            )
        draw.text((20, y_ch), "# " + ch, fill=INDIGO_600 if i == 1 else GRAY_600, font=get_font(13))
        y_ch += 38

    # Chat area
    x_chat = 280
    y_chat = 68

    # User message
    draw.rounded_rectangle(
        [(x_chat, y_chat), (x_chat + 500, y_chat + 60)],
        radius=12,
        fill=WHITE,
    )
    draw.text((x_chat + 16, y_chat + 8), "Sarah Chen", fill=GRAY_900, font=get_font(12, bold=True))
    draw.text((x_chat + 120, y_chat + 10), "2:34 PM", fill=GRAY_400, font=get_font(10))
    draw.text(
        (x_chat + 16, y_chat + 30),
        'What\'s our total x402 revenue this month?',
        fill=GRAY_700,
        font=get_font(14),
    )

    # Copilot response
    y_bot = y_chat + 80
    draw.rounded_rectangle(
        [(x_chat, y_bot), (w - 40, y_bot + 330)],
        radius=12,
        fill=WHITE,
    )

    # Bot header
    draw.rounded_rectangle(
        [(x_chat + 12, y_bot + 10), (x_chat + 34, y_bot + 32)],
        radius=4,
        fill=TEAL_500,
    )
    draw.text((x_chat + 16, y_bot + 13), "A", fill=WHITE, font=get_font(12, bold=True))
    draw.text((x_chat + 42, y_bot + 12), "AgentRails FinanceOps", fill=GRAY_900, font=get_font(12, bold=True))
    draw.text((x_chat + 230, y_bot + 14), "2:34 PM", fill=GRAY_400, font=get_font(10))

    y_resp = y_bot + 44
    draw.text(
        (x_chat + 16, y_resp),
        "Here's your x402 revenue summary for February 2026:",
        fill=GRAY_700,
        font=get_font(13),
    )

    # Revenue card
    y_card = y_resp + 30
    draw.rounded_rectangle(
        [(x_chat + 16, y_card), (x_chat + 600, y_card + 180)],
        radius=8,
        fill=GRAY_50,
        outline=GRAY_200,
    )

    # Revenue stats
    stats = [
        ("Total Revenue", "$12,847.32"),
        ("Transactions", "298,412"),
        ("Unique Agents", "47"),
        ("Avg per Tx", "$0.043"),
    ]
    sx = x_chat + 32
    for label, val in stats:
        draw.text((sx, y_card + 16), label, fill=GRAY_500, font=get_font(11))
        draw.text((sx, y_card + 34), val, fill=GRAY_900, font=get_font(18, bold=True))
        sx += 150

    # Mini bar chart
    bars_y = y_card + 80
    draw.text((x_chat + 32, bars_y), "Daily Revenue (last 7 days)", fill=GRAY_500, font=get_font(10))
    bars_y += 20
    daily_values = [420, 380, 510, 490, 620, 580, 650]
    max_val = max(daily_values)
    for i, val in enumerate(daily_values):
        bar_h = int(val / max_val * 55)
        bx = x_chat + 32 + i * 82
        draw.rounded_rectangle(
            [(bx, bars_y + 55 - bar_h), (bx + 60, bars_y + 55)],
            radius=4,
            fill=INDIGO_500,
        )
        draw.text((bx + 20, bars_y + 60), f"Feb {17 + i}", fill=GRAY_400, font=get_font(9))

    # Top agents breakdown
    y_agents = y_card + 195
    draw.text(
        (x_chat + 16, y_agents),
        "Top spending agents: research-agent-01 ($4,231), finance-copilot ($2,891), data-agent-03 ($1,847)",
        fill=GRAY_600,
        font=get_font(12),
    )

    # Second user message
    y_msg2 = y_bot + 350
    draw.rounded_rectangle(
        [(x_chat, y_msg2), (x_chat + 600, y_msg2 + 60)],
        radius=12,
        fill=WHITE,
    )
    draw.text((x_chat + 16, y_msg2 + 8), "Sarah Chen", fill=GRAY_900, font=get_font(12, bold=True))
    draw.text((x_chat + 120, y_msg2 + 10), "2:35 PM", fill=GRAY_400, font=get_font(10))
    draw.text(
        (x_chat + 16, y_msg2 + 30),
        "Are any agents over their spending limits?",
        fill=GRAY_700,
        font=get_font(14),
    )

    # Input bar
    draw.rounded_rectangle(
        [(x_chat, h - 56), (w - 20, h - 12)],
        radius=8,
        fill=WHITE,
        outline=GRAY_200,
    )
    draw.text((x_chat + 16, h - 42), "Ask AgentRails FinanceOps a question...", fill=GRAY_400, font=get_font(13))

    img.save(os.path.join(OUT, "screenshot-4-copilot.png"))
    print("  Created screenshot-4-copilot.png")


def screenshot_5_flow(w=1280, h=720):
    """Screenshot 5: Payment Flow — visual diagram."""
    img = gradient_image((w, h), (248, 250, 252), WHITE, "vertical")
    draw = ImageDraw.Draw(img)

    # Title
    font_title = get_font(32, bold=True)
    draw.text((w // 2 - 220, 30), "How x402 Payments Work", fill=GRAY_900, font=font_title)
    font_sub = get_font(16)
    draw.text(
        (w // 2 - 260, 72),
        "HTTP 402 is the new API key — one protocol, every framework",
        fill=GRAY_500,
        font=font_sub,
    )

    # Flow boxes
    box_w = 240
    box_h = 140
    gap = 60
    total_w = 4 * box_w + 3 * gap
    start_x = (w - total_w) // 2
    box_y = 160

    steps = [
        ("1. Agent Calls API", "Standard HTTP request.\nNo API key.\nNo signup.", INDIGO_500),
        ("2. Server Returns 402", "Payment Required header\nwith price, currency,\nand receiver address.", AMBER_400),
        ("3. Agent Pays", "SDK checks budget,\nsigns USDC authorization,\nretries automatically.", CYAN_500),
        ("4. Data Returned", "API delivers response.\nPayment settles on-chain.\nAgent moves on.", EMERALD_500),
    ]

    for i, (title, desc, color) in enumerate(steps):
        bx = start_x + i * (box_w + gap)

        # Box
        draw.rounded_rectangle(
            [(bx, box_y), (bx + box_w, box_y + box_h)],
            radius=12,
            fill=WHITE,
            outline=GRAY_200,
        )

        # Color top bar
        draw.rounded_rectangle(
            [(bx, box_y), (bx + box_w, box_y + 6)],
            radius=3,
            fill=color,
        )

        # Step number circle
        draw.ellipse(
            [(bx + box_w // 2 - 16, box_y - 16), (bx + box_w // 2 + 16, box_y + 16)],
            fill=color,
        )
        draw.text(
            (bx + box_w // 2 - 5, box_y - 10),
            str(i + 1),
            fill=WHITE,
            font=get_font(16, bold=True),
        )

        # Title
        draw.text((bx + 16, box_y + 24), title.split(". ")[1], fill=GRAY_900, font=get_font(15, bold=True))

        # Description lines
        y_desc = box_y + 48
        for line in desc.split("\n"):
            draw.text((bx + 16, y_desc), line, fill=GRAY_600, font=get_font(12))
            y_desc += 18

        # Arrow between boxes
        if i < 3:
            ax = bx + box_w + 8
            ay = box_y + box_h // 2
            draw.line([(ax, ay), (ax + gap - 16, ay)], fill=GRAY_300, width=2)
            # Arrowhead
            draw.polygon(
                [(ax + gap - 16, ay - 6), (ax + gap - 4, ay), (ax + gap - 16, ay + 6)],
                fill=GRAY_300,
            )

    # Code example below
    code_y = box_y + box_h + 50
    code_x = start_x
    code_w_total = total_w

    draw.rounded_rectangle(
        [(code_x, code_y), (code_x + code_w_total, code_y + 280)],
        radius=12,
        fill=INDIGO_900,
    )

    # Window bar
    draw.rounded_rectangle(
        [(code_x, code_y), (code_x + code_w_total, code_y + 36)],
        radius=12,
        fill=(30, 30, 50),
    )
    draw.rectangle([(code_x, code_y + 24), (code_x + code_w_total, code_y + 36)], fill=(30, 30, 50))
    for i, color in enumerate([RED_400, AMBER_400, GREEN_400]):
        draw.ellipse(
            [(code_x + 14 + i * 20, code_y + 10), (code_x + 26 + i * 20, code_y + 22)],
            fill=color,
        )

    # Two-column code
    mono = get_mono_font(12)
    half = code_w_total // 2

    # Left: Server side
    draw.text((code_x + 20, code_y + 46), "Server (Python — FastAPI)", fill=GRAY_400, font=get_font(11, bold=True))
    server_lines = [
        [("@app", CYAN_400), (".get(", WHITE), ('"/api/data"', EMERALD_500), (")", WHITE)],
        [("@x402_protected", VIOLET_500), ("(price=", WHITE), ("0.01", AMBER_400), (")", WHITE)],
        [("async def", VIOLET_500), (" get_data():", WHITE)],
        [("    return ", WHITE), ('{"data": "premium"}', EMERALD_500)],
    ]
    y_cl = code_y + 70
    for parts in server_lines:
        x_cl = code_x + 20
        for text, clr in parts:
            draw.text((x_cl, y_cl), text, fill=clr, font=mono)
            bbox = draw.textbbox((0, 0), text, font=mono)
            x_cl += bbox[2] - bbox[0]
        y_cl += 20

    # Divider
    draw.line(
        [(code_x + half, code_y + 42), (code_x + half, code_y + 270)],
        fill=(60, 60, 80),
        width=1,
    )

    # Right: Agent side
    draw.text((code_x + half + 20, code_y + 46), "Agent (Python — LangChain)", fill=GRAY_400, font=get_font(11, bold=True))
    agent_lines = [
        [("from", VIOLET_500), (" langchain_x402 ", WHITE), ("import", VIOLET_500), (" X402Toolkit", CYAN_400)],
        [],
        [("toolkit = X402Toolkit(", WHITE), ("key", AMBER_400), ("=", WHITE), ("pk", EMERALD_500), (")", WHITE)],
        [("result = agent.invoke(", WHITE), ('"Get data"', EMERALD_500), (")", WHITE)],
        [("print(result)", WHITE), ("  # {data: premium}", GRAY_500)],
    ]
    y_cl = code_y + 70
    for parts in agent_lines:
        x_cl = code_x + half + 20
        for text, clr in parts:
            draw.text((x_cl, y_cl), text, fill=clr, font=mono)
            bbox = draw.textbbox((0, 0), text, font=mono)
            x_cl += bbox[2] - bbox[0]
        y_cl += 20

    # Bottom: flow annotation
    flow_y = code_y + 180
    draw.text((code_x + 20, flow_y), "# Flow: Request", fill=GRAY_500, font=mono)
    draw.text((code_x + 20, flow_y + 18), "#   1. Agent calls GET /api/data", fill=GRAY_500, font=mono)
    draw.text((code_x + 20, flow_y + 36), "#   2. Server returns 402 + price", fill=GRAY_500, font=mono)
    draw.text((code_x + 20, flow_y + 54), "#   3. SDK pays $0.01 USDC, retries", fill=GRAY_500, font=mono)

    draw.text((code_x + half + 20, flow_y), "#   4. Server verifies payment", fill=GRAY_500, font=mono)
    draw.text((code_x + half + 20, flow_y + 18), "#   5. Returns premium data", fill=GRAY_500, font=mono)
    draw.text((code_x + half + 20, flow_y + 36), "#   6. Settles on-chain (USDC)", fill=GRAY_500, font=mono)

    img.save(os.path.join(OUT, "screenshot-5-flow.png"))
    print("  Created screenshot-5-flow.png")


def video_thumbnail(w=1280, h=720):
    """Video thumbnail."""
    img = gradient_image((w, h), INDIGO_900, (20, 15, 60), "diagonal")
    draw = ImageDraw.Draw(img)

    # Play button circle
    cx, cy = w // 2, h // 2 - 20
    r = 50
    draw.ellipse([(cx - r, cy - r), (cx + r, cy + r)], fill=WHITE)
    # Play triangle
    draw.polygon(
        [(cx - 15, cy - 25), (cx - 15, cy + 25), (cx + 25, cy)],
        fill=INDIGO_600,
    )

    # Title
    font_title = get_font(36, bold=True)
    draw.text((w // 2 - 300, cy + 80), "AgentRails — Platform Demo", fill=WHITE, font=font_title)
    font_sub = get_font(18)
    draw.text(
        (w // 2 - 260, cy + 130),
        "AI Agent Payment Infrastructure  |  x402 Protocol  |  Microsoft 365",
        fill=GRAY_400,
        font=font_sub,
    )

    # Duration badge
    draw.rounded_rectangle(
        [(w - 100, h - 40), (w - 20, h - 14)],
        radius=4,
        fill=(0, 0, 0),
    )
    draw.text((w - 90, h - 37), "4:12", fill=WHITE, font=get_font(14, bold=True))

    img.save(os.path.join(OUT, "video-thumbnail.png"))
    print("  Created video-thumbnail.png")


# =============================================================================
# MAIN
# =============================================================================

if __name__ == "__main__":
    print("Generating Microsoft Marketplace assets...\n")

    print("Logos:")
    generate_logo(300, "logo-300x300.png")
    generate_logo(90, "logo-90x90.png")
    generate_logo(48, "logo-48x48.png")

    print("\nScreenshots:")
    screenshot_1_swagger()
    screenshot_2_sdk()
    screenshot_3_dashboard()
    screenshot_4_copilot()
    screenshot_5_flow()

    print("\nVideo thumbnail:")
    video_thumbnail()

    print(f"\nAll assets saved to: {OUT}")
