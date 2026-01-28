# AgentRails Dashboard Deployment Guide

Deploy the enterprise dashboard to `app.agentrails.io` using Porkbun DNS and Vercel.

---

## Architecture Overview

```
www.agentrails.io      → Marketing site (Vercel)
app.agentrails.io      → Enterprise dashboard (Vercel) ← THIS GUIDE
api.agentrails.io      → REST API (Render)
```

---

## Step 1: Porkbun DNS Configuration

### 1.1 Login to Porkbun
1. Go to [porkbun.com](https://porkbun.com)
2. Login to your account
3. Click **Domain Management**
4. Find `agentrails.io` and click **DNS**

### 1.2 Add DNS Record for app.agentrails.io

You'll add the DNS record **after** deploying to Vercel (Step 2), because Vercel will give you the values. Skip to Step 2 first, then come back here.

**After Vercel deployment, add this record:**

| Type | Host | Answer | TTL |
|------|------|--------|-----|
| CNAME | app | cname.vercel-dns.com | 600 |

**How to add:**
1. In Porkbun DNS page, scroll to **Add Record**
2. Select **CNAME** from the dropdown
3. Enter:
   - **Host**: `app`
   - **Answer**: `cname.vercel-dns.com`
   - **TTL**: `600` (or default)
4. Click **Add**

---

## Step 2: Vercel Deployment

### 2.1 Prepare the Dashboard Repository

**Option A: Use the dashboard-app folder (Recommended)**

The dashboard files are ready in: `dashboard-app/`

```
dashboard-app/
├── index.html      # Main dashboard
├── login.html      # Login page
└── vercel.json     # Routing config
```

**Option B: Deploy from a separate Git repo**

1. Create a new GitHub repo: `agentrails-dashboard`
2. Copy the `dashboard-app/` contents to it
3. Push to GitHub

### 2.2 Deploy to Vercel

#### Method 1: Vercel CLI (Fastest)

```bash
# Install Vercel CLI
npm install -g vercel

# Navigate to dashboard folder
cd dashboard-app

# Deploy
vercel

# Follow prompts:
# - Link to existing project? No
# - What's your project name? agentrails-dashboard
# - Which directory? ./
# - Override settings? No
```

#### Method 2: Vercel Web Dashboard

1. Go to [vercel.com](https://vercel.com)
2. Click **Add New** → **Project**
3. Choose **Import Git Repository** or **Upload Folder**
4. If uploading:
   - Drag and drop the `dashboard-app` folder
   - Or click to browse and select it
5. Configure:
   - **Project Name**: `agentrails-dashboard`
   - **Framework**: `Other`
   - **Root Directory**: `./`
6. Click **Deploy**

### 2.3 Add Custom Domain in Vercel

1. After deployment, go to your project in Vercel
2. Click **Settings** → **Domains**
3. Enter: `app.agentrails.io`
4. Click **Add**
5. Vercel will show DNS configuration instructions
6. **Now go back to Step 1.2** and add the CNAME record in Porkbun

### 2.4 Wait for DNS Propagation

- Usually takes 5-10 minutes
- Can take up to 48 hours in some cases
- Check status in Vercel's Domains page (will show green checkmark when ready)

### 2.5 Enable HTTPS (Automatic)

Vercel automatically provisions SSL certificates. Once DNS propagates:
- `https://app.agentrails.io` will work
- HTTP will auto-redirect to HTTPS

---

## Step 3: Configure CORS on API (Render)

Your API at `api.agentrails.io` needs to allow requests from `app.agentrails.io`.

### 3.1 Update CORS in Program.cs

The current CORS config allows all origins. For production, you may want to restrict it:

```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
            "https://app.agentrails.io",
            "https://www.agentrails.io",
            "http://localhost:5173",  // Local dev
            "https://localhost:7098"  // Local API
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});
```

### 3.2 Deploy API Update to Render

After updating CORS:
1. Commit and push to your API repo
2. Render will auto-deploy (if connected to Git)
3. Or manually trigger deploy in Render dashboard

---

## Step 4: Verify Deployment

### 4.1 Test the Dashboard

1. Go to `https://app.agentrails.io`
2. You should see the login page
3. Register a new account or login
4. Verify dashboard loads and shows data

### 4.2 Test API Connectivity

Open browser console (F12) and check:
- No CORS errors
- API calls going to `https://api.agentrails.io`
- Authentication working

### 4.3 Troubleshooting

| Issue | Solution |
|-------|----------|
| DNS not resolving | Wait for propagation, check Porkbun record |
| CORS errors | Check API CORS config, redeploy |
| Login fails | Check API is running, check network tab |
| SSL error | Wait for Vercel to provision certificate |

---

## File Structure Summary

```
agentrails.io/
├── www (Vercel - Marketing)
│   └── Your marketing site
│
├── app (Vercel - Dashboard)      ← This deployment
│   ├── index.html                # Dashboard SPA
│   ├── login.html                # Login/Register
│   └── vercel.json               # Routing rules
│
└── api (Render - Backend)
    └── AgenticCommerce.API       # .NET API
```

---

## Quick Reference

| Service | URL | Platform |
|---------|-----|----------|
| Marketing | https://www.agentrails.io | Vercel |
| Dashboard | https://app.agentrails.io | Vercel |
| API | https://api.agentrails.io | Render |
| Swagger | https://api.agentrails.io/swagger | Render |

---

## Updating the Dashboard

To deploy updates:

### Via Vercel CLI
```bash
cd dashboard-app
vercel --prod
```

### Via Git (if connected)
```bash
git add .
git commit -m "Update dashboard"
git push
# Vercel auto-deploys on push
```

### Via Vercel Dashboard
1. Go to vercel.com → Your project
2. Click **Deployments**
3. Click **Redeploy** on latest
