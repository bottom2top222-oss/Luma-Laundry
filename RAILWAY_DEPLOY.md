# Railway.app Deployment Guide for Luma Laundry

This guide walks through deploying your Luma Laundry application to Railway.app with your custom domain: **luma-laundry.app**

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

## Step 3: Configure Environment Variables

1. In your Railway project dashboard, click on your service

2. Go to the **"Variables"** tab

3. Add these environment variables:
   ```
   ASPNETCORE_ENVIRONMENT=Production
   ```

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
  - Email: `admin@laundry.com`
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

### Domain not working:
- DNS can take time to propagate (up to 48 hours)
- Use `nslookup luma-laundry.app` to check DNS status
- Verify DNS records match Railway's requirements exactly

### Database resets on deploy:
- Ensure the volume is properly configured
- Check that `ConnectionStrings:IdentityConnection` points to `/var/data/laundry.db`

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
- [ ] Add your real support email (currently `support@luma.com`)

## Next Steps

1. Test all features on the live site
2. Create regular users and test ordering flow
3. Monitor logs for any production errors
4. Set up email service for order confirmations (future enhancement)
5. Consider adding database backups via Railway CLI

---

**Support**: If you encounter issues, Railway has excellent documentation at https://docs.railway.app and community Discord support.

🌊 **Luma is ready to launch!**
