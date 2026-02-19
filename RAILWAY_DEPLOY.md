# Railway.app Deployment Guide for Luma Laundry

This guide walks through deploying your Luma Laundry application to Railway.app with your custom domain: **luma-laundry.app**

## Release Notes (Latest)

### 2026-02-19

- Commit: `be3b30e`
- Restyled the user dashboard to match the new dark blue/cyan visual direction.
- Updated dashboard actions to button-like tiles for improved clarity and consistency.
- Validation completed before push:
   - `dotnet build LaundryApp.sln -v minimal` → build successful
- Railway impact: redeploy `frontend` to apply the dashboard UI update.

### 2026-02-19

- Commit: `de42945`
- Fixed schedule flow lookup fallback to prevent dead-end navigation after scheduling.
- Improved resilience when transitioning from scheduling to payment/order continuation.
- Validation completed before push:
   - `dotnet build LaundryApp.sln -v minimal` → build successful
- Railway impact: redeploy `frontend` to apply schedule flow reliability fixes.

### 2026-02-19

- Commit: `56a514b`
- Set `Support@luma-laundry.app` as the default admin email in app configuration.
- Updated identity seeding logic to synchronize an existing Admin account to the configured admin email when safe.
- Validation completed before push:
   - `dotnet build LaundryApp.sln -v minimal` → build successful
   - `dotnet test LaundryApp.sln -v minimal` → all tests passing (3/3)
- Railway impact: redeploy `frontend` and `backend-server` so startup seeding applies the admin email update.

### 2026-02-19

- Commit: `74fc4a3`
- Added forgot-password and reset-password flow for admin and user accounts.
- Added reset-link email delivery through SMTP (`Email:*` configuration).
- Added new account pages: forgot password form, reset form, and confirmation screens.
- Validation completed before push:
   - `dotnet build LaundryApp.sln -v minimal` → build successful
   - `dotnet test LaundryApp.sln -v minimal` → all tests passing (3/3)
- Railway impact: set SMTP variables (`Email__SmtpHost`, `Email__SmtpPort`, `Email__FromAddress`, `Email__FromName`, `Email__Username`, `Email__Password`, `Email__EnableSsl`) and redeploy `frontend`.

### 2026-02-19

- Commit: `0363f61`
- Hardened authentication and admin access security.
- Added stronger Identity password policy requirements (uppercase/lowercase/number/symbol, minimum length 8).
- Enabled account lockout after repeated failed sign-in attempts.
- Added anti-forgery validation/tokens to login, register, and logout flows.
- Added `Change Password` screen and forced admin password rotation when default admin credentials are still in use.
- Validation completed before push:
   - `dotnet build LaundryApp.sln -v minimal` → build successful
   - `dotnet test LaundryApp.sln -v minimal` → all tests passing (3/3)
- Railway impact: no new variables required; redeploy `frontend` to apply the updated login/password behavior.

### 2026-02-19

- Commit: `53563f9`
- Added starter automated tests (xUnit) and wired them into the solution.
- Remediated vulnerable NuGet dependencies by upgrading to secure .NET 8 patch versions.
- Validation completed before push:
   - `dotnet list LaundryApp.sln package --vulnerable --include-transitive` → no vulnerable packages
   - `dotnet test LaundryApp.sln -v minimal` → all tests passing (3/3)
- Railway impact: no variable changes required; redeploy `frontend`, `backend-server`, and `backend-worker` to pick up package updates.

## Prerequisites

- GitHub account
- Railway.app account (sign up at https://railway.app)
- Your domain registrar access to configure DNS records

## Step 1: Push Code to GitHub

1. Initialize a Git repository (if not already done):
   ```bash
   git init
   git add .
   git commit -m "Initial commit - Luma Laundry App"
   ```

2. Create a new repository on GitHub (https://github.com/new)
   - Name it `luma-laundry` or similar
   - Don't initialize with README (you already have code)

3. Push your code:
   ```bash
   git remote add origin https://github.com/YOUR_USERNAME/luma-laundry.git
   git branch -M main
   git push -u origin main
   ```

## Step 2: Deploy to Railway

1. Go to https://railway.app and sign in with GitHub

2. Click **"New Project"**

3. Select **"Deploy from GitHub repo"**

4. Choose your `luma-laundry` repository

5. Railway will automatically detect the `railway.json` and `Dockerfile`

6. Click **Deploy**

## Step 2B (Recommended): Split into 3 Railway Services

For the architecture shown in your Railway screenshot, create **three services** from the same repo:

1. **frontend** (public)
   - Dockerfile path: `docker/Dockerfile.frontend`
   - Expose HTTP (Railway will route to port `8080`)

2. **backend-server** (private/internal)
   - Dockerfile path: `docker/Dockerfile.api`
   - Expose HTTP internally (port `8080`)

3. **backend-worker** (private/internal)
   - Dockerfile path: `docker/Dockerfile.worker`
   - No public domain required

You can keep your existing root `Dockerfile` for legacy single-service deploys, but the `docker/*` files are the new multi-service setup.

### Service Variable Matrix

Set variables per service in Railway:

#### `frontend`
```
ASPNETCORE_ENVIRONMENT=Production
LayeredServices__ApiOnlyMode=true
LayeredServices__ApiBaseUrl=http://backend-server.railway.internal:8080
Database__Path=/var/data/laundry.db
```

#### `backend-server`
```
ASPNETCORE_ENVIRONMENT=Production
Database__Path=/var/data/laundry.db
```

#### `backend-worker`
```
ASPNETCORE_ENVIRONMENT=Production
LayeredServices__ApiBaseUrl=http://backend-server.railway.internal:8080
```

> Tip: internal Railway hostnames typically resolve as `<service-name>.railway.internal`.

### Ready-to-Paste Variables

Use these blocks directly when configuring each Railway service.

#### frontend (.env style)
```
ASPNETCORE_ENVIRONMENT=Production
LayeredServices__ApiOnlyMode=true
LayeredServices__ApiBaseUrl=http://backend-server.railway.internal:8080
Database__Path=/var/data/laundry.db
```

#### backend-server (.env style)
```
ASPNETCORE_ENVIRONMENT=Production
Database__Path=/var/data/laundry.db
```

#### backend-worker (.env style)
```
ASPNETCORE_ENVIRONMENT=Production
LayeredServices__ApiBaseUrl=http://backend-server.railway.internal:8080
```

#### frontend (JSON-style)
```json
{
   "ASPNETCORE_ENVIRONMENT": "Production",
   "LayeredServices__ApiOnlyMode": "true",
   "LayeredServices__ApiBaseUrl": "http://backend-server.railway.internal:8080",
   "Database__Path": "/var/data/laundry.db"
}
```

#### backend-server (JSON-style)
```json
{
   "ASPNETCORE_ENVIRONMENT": "Production",
   "Database__Path": "/var/data/laundry.db"
}
```

#### backend-worker (JSON-style)
```json
{
   "ASPNETCORE_ENVIRONMENT": "Production",
   "LayeredServices__ApiBaseUrl": "http://backend-server.railway.internal:8080"
}
```

## Railway Click-Path Checklist (3-Service Setup)

Use this exact sequence in Railway UI.

### A) Create Services

1. Open your Railway project.
2. Click **+ New** → **GitHub Repo** (same repo each time).
3. Create service **frontend**.
4. Repeat and create service **backend-server**.
5. Repeat and create service **backend-worker**.

### B) Set Dockerfile Per Service

For each service: **Service → Settings → Source → Root Directory / Dockerfile Path**

- **frontend** → `docker/Dockerfile.frontend`
- **backend-server** → `docker/Dockerfile.api`
- **backend-worker** → `docker/Dockerfile.worker`

### C) Networking / Exposure

- **frontend**: Public domain enabled (this is your website).
- **backend-server**: Keep private (no public domain).
- **backend-worker**: Keep private (no public domain).

### D) Variables (exact values)

#### frontend
```
ASPNETCORE_ENVIRONMENT=Production
LayeredServices__ApiOnlyMode=true
LayeredServices__ApiBaseUrl=http://backend-server.railway.internal:8080
Database__Path=/var/data/laundry.db
```

#### backend-server
```
ASPNETCORE_ENVIRONMENT=Production
Database__Path=/var/data/laundry.db
```

#### backend-worker
```
ASPNETCORE_ENVIRONMENT=Production
LayeredServices__ApiBaseUrl=http://backend-server.railway.internal:8080
```

### E) Volume Mounts

- Attach volume to **frontend** at `/var/data`.
- Attach volume to **backend-server** at `/var/data`.
- Worker does not require DB volume for current architecture.

### F) Deploy + Verify

1. Trigger deploy for all three services.
2. Check logs until each shows healthy startup.
3. Open frontend domain and verify homepage loads.
4. Verify internal API connectivity by checking frontend logs for missing API connection errors.

### G) Common Misconfigurations

- Wrong Dockerfile path per service.
- Public domain accidentally enabled for backend-server/worker.
- Missing `LayeredServices__ApiBaseUrl` on frontend/worker.
- Missing volume mount at `/var/data` for services writing SQLite.

## Step 3: Configure Environment Variables

1. In your Railway project dashboard, click on your service

2. Go to the **"Variables"** tab

3. Add these environment variables:
   ```
   ASPNETCORE_ENVIRONMENT=Production
   Database__Path=/var/data/laundry.db
   ```

If you use multi-service deployment, apply the variables from **Step 2B** to each service instead of one shared set.

4. Click **"Deploy"** to restart with new variables

## Step 4: Add Persistent Storage

Your SQLite database needs persistent storage:

1. In your Railway service, go to **"Settings"**

2. Scroll to **"Volumes"**

3. Click **"+ New Volume"**

4. Configure:
   - **Mount Path**: `/var/data`
   - This ensures your database persists across deployments

5. Click **"Add"** - Railway will redeploy automatically

## Step 5: Custom Domain Configuration

### On Railway:

1. In your service, go to **"Settings"**

2. Scroll to **"Domains"** section

3. Click **"+ Custom Domain"**

4. Enter: `luma-laundry.app`

5. Railway will show you the required DNS records:
   - Type: `CNAME` or `A` 
   - Value will be something like: `your-app.up.railway.app`

### On Your Domain Registrar:

1. Log into your domain registrar where you bought `luma-laundry.app`

2. Go to DNS settings

3. Add the records Railway provided:
   
   **For root domain (luma-laundry.app):**
   - Type: `A`
   - Name/Host: `@`
   - Value: (IP address provided by Railway)
   - TTL: 3600 (or default)

   **For www subdomain (optional):**
   - Type: `CNAME`
   - Name/Host: `www`
   - Value: (Railway domain provided)
   - TTL: 3600

4. Save the DNS changes

5. **Wait for DNS propagation** (can take 5 minutes to 48 hours, usually ~1 hour)

### Verify Custom Domain:

1. Back in Railway, the domain status will change to **"Active"** when DNS propagates

2. Railway automatically provisions SSL certificates (HTTPS)

3. Visit https://luma-laundry.app - your site should be live!

## Step 6: Initialize Database

The application will automatically:
- Run migrations on startup (see `Program.cs`)
- Create the admin user with credentials:
   - Email: `Support@luma-laundry.app`
  - Password: `Admin123!`

**Important**: Change the admin password after first login!

## Monitoring & Logs

### View Logs:
1. In Railway dashboard, click your service
2. Go to **"Deployments"** tab
3. Click on the active deployment
4. View real-time logs

### Check Build Status:
- Deployments tab shows all builds
- Green checkmark = successful deployment
- Red X = build failed (click for logs)

### Database Access:
Railway doesn't provide direct SQLite browser, but you can:
1. Download the database via Railway CLI
2. Or add a simple admin panel to view data

## Updating Your Application

Every time you push to GitHub:

```bash
git add .
git commit -m "Your update message"
git push
```

Railway automatically detects the push and redeploys!

## Troubleshooting

### App won't start:
- Check logs in Railway dashboard
- Verify environment variables are set
- Ensure volume is mounted to `/var/data`
- Confirm `Database__Path=/var/data/laundry.db` is set

### Domain not working:
- DNS can take time to propagate (up to 48 hours)
- Use `nslookup luma-laundry.app` to check DNS status
- Verify DNS records match Railway's requirements exactly

### Database resets on deploy:
- Ensure the volume is properly configured
- Check that `Database__Path` points to `/var/data/laundry.db`

### 500 errors:
- Check logs for exceptions
- Common issue: Database file permissions
- Solution already in Dockerfile: `chmod 777 /var/data`

## Railway CLI (Optional)

For advanced management, install Railway CLI:

```bash
# Install
npm i -g @railway/cli

# Login
railway login

# Link to project
railway link

# View logs
railway logs

# Run commands in production
railway run dotnet ef database update
```

## Cost Estimate

Railway.app pricing (as of 2026):
- **Starter Plan**: $5/month (includes $5 usage credit)
- **Usage**: ~$0.000231/min for your app size
- **Estimate**: Hobby projects typically stay under $5/month

Free trial available to test deployment first!

## Security Checklist

Before going live:
- [ ] Change admin password from default
- [ ] Review `appsettings.Production.json` for allowed hosts
- [ ] Enable HTTPS-only (already configured in `Program.cs`)
- [ ] Set strong cookie encryption keys (auto-generated)
- [ ] Review Terms of Service and Privacy Policy content
- [ ] Verify support/admin email is set to `Support@luma-laundry.app`

## Post-Deploy Verification (Auth Hardening)

After redeploying `frontend`, verify the new security behavior end-to-end:

1. Open the live site and log in as admin using the default password.
2. Confirm you are redirected to `/Account/ChangePassword` before accessing dashboard pages.
3. Attempt to set the new password back to `Admin123!` and confirm it is rejected.
4. Set a valid strong password and confirm successful redirect to dashboard.
5. Log out and log in with the new password to confirm persistence.
6. Submit login with a wrong password repeatedly and confirm account lockout message appears after threshold is reached.
7. Confirm authenticated logout still works from the top-right menu.

Expected result: default admin credentials are no longer usable after rotation, and repeated failed sign-ins trigger temporary lockout.

## Next Steps

1. Test all features on the live site
2. Create regular users and test ordering flow
3. Monitor logs for any production errors
4. Set up email service for order confirmations (future enhancement)
5. Consider adding database backups via Railway CLI

---

**Support**: If you encounter issues, Railway has excellent documentation at https://docs.railway.app and community Discord support.

🌊 **Luma is ready to launch!**
