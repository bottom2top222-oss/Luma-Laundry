# Azure App Service Deployment for Luma

## Prerequisites
- Azure account (free tier available at https://azure.microsoft.com/free)
- Azure CLI installed

## Step 1: Install Azure CLI (if not already installed)
```powershell
winget install Microsoft.AzureCLI
# or
# Download from: https://aka.ms/installazurecliwindows
```

## Step 2: Login to Azure
```powershell
az login
```

## Step 3: Create Resources
```powershell
# Set variables
$resourceGroup = "luma-rg"
$location = "eastus"
$appServicePlan = "luma-plan"
$webAppName = "luma-laundry"  # Must be globally unique

# Create resource group
az group create --name $resourceGroup --location $location

# Create App Service plan (B1 basic tier - ~$13/month)
az appservice plan create `
  --name $appServicePlan `
  --resource-group $resourceGroup `
  --sku B1 `
  --is-linux

# Create web app
az webapp create `
  --resource-group $resourceGroup `
  --plan $appServicePlan `
  --name $webAppName `
  --runtime "DOTNET:8.0"
```

## Step 4: Configure App Settings
```powershell
# Set environment to Production
az webapp config appsettings set `
  --resource-group $resourceGroup `
  --name $webAppName `
  --settings ASPNETCORE_ENVIRONMENT=Production

# Configure SQLite path for Linux App Service persistent storage
az webapp config appsettings set `
  --resource-group $resourceGroup `
  --name $webAppName `
  --settings Database__Path=/home/site/wwwroot/App_Data/laundry.db

# Enable detailed error messages (optional, for debugging)
az webapp config appsettings set `
  --resource-group $resourceGroup `
  --name $webAppName `
  --settings ASPNETCORE_DETAILEDERRORS=true
```

## Step 5: Deploy the Application
```powershell
# Publish the app
dotnet publish -c Release -o ./publish

# Create deployment package
Compress-Archive -Path ./publish/* -DestinationPath deploy.zip -Force

# Deploy to Azure
az webapp deployment source config-zip `
  --resource-group $resourceGroup `
  --name $webAppName `
  --src deploy.zip

# Clean up
Remove-Item deploy.zip
Remove-Item -Recurse ./publish
```

## Step 6: Verify Deployment
```powershell
# Get the app URL
az webapp show --resource-group $resourceGroup --name $webAppName --query defaultHostName --output tsv
```

Visit: https://{your-app-name}.azurewebsites.net

## Step 7: Add Custom Domain (luma-laundry.app)

### Option A: Using Azure Portal
1. Go to Azure Portal (portal.azure.com)
2. Navigate to your App Service
3. Select "Custom domains" from the left menu
4. Click "Add custom domain"
5. Enter `luma-laundry.app`
6. Follow DNS verification steps
7. Add SSL certificate (free with App Service Managed Certificate)

### Option B: Using Azure CLI
```powershell
# Get the custom domain verification ID
$verificationId = az webapp show `
  --resource-group $resourceGroup `
  --name $webAppName `
  --query customDomainVerificationId `
  --output tsv

Write-Host "Add these DNS records to your domain:"
Write-Host "TXT record: asuid.luma-laundry.app -> $verificationId"
Write-Host "CNAME record: www.luma-laundry.app -> $webAppName.azurewebsites.net"
Write-Host "A record: @ -> (IP from portal or use CNAME to @.azurewebsites.net)"

# After DNS records are added, map the domain
az webapp config hostname add `
  --resource-group $resourceGroup `
  --webapp-name $webAppName `
  --hostname luma-laundry.app

# Enable HTTPS
az webapp update `
  --resource-group $resourceGroup `
  --name $webAppName `
  --https-only true

# Create and bind free managed certificate
az webapp config ssl create `
  --resource-group $resourceGroup `
  --name $webAppName `
  --hostname luma-laundry.app

az webapp config ssl bind `
  --resource-group $resourceGroup `
  --name $webAppName `
  --certificate-thumbprint (get from previous command) `
  --ssl-type SNI
```

## Step 8: Configure Production Database

### Option A: Keep SQLite (simpler, good for low traffic)
```powershell
# Enable persistent storage
az webapp config appsettings set `
  --resource-group $resourceGroup `
  --name $webAppName `
  --settings WEBSITES_ENABLE_APP_SERVICE_STORAGE=true

# Ensure app uses persistent SQLite location
az webapp config appsettings set `
  --resource-group $resourceGroup `
  --name $webAppName `
  --settings Database__Path=/home/site/wwwroot/App_Data/laundry.db
```

### Option B: Upgrade to Azure SQL Database (recommended for production)
```powershell
# Create SQL Server
az sql server create `
  --name luma-sql-server `
  --resource-group $resourceGroup `
  --location $location `
  --admin-user lumadmin `
  --admin-password 'YourSecurePassword123!'

# Create database
az sql db create `
  --resource-group $resourceGroup `
  --server luma-sql-server `
  --name lumadb `
  --service-objective S0

# Get connection string and add to app settings
# Then update Program.cs to use SQL Server instead of SQLite
```

## Monitoring & Logs
```powershell
# Enable logging
az webapp log config `
  --resource-group $resourceGroup `
  --name $webAppName `
  --application-logging filesystem `
  --level information

# Stream logs
az webapp log tail `
  --resource-group $resourceGroup `
  --name $webAppName

# View deployment history
az webapp deployment list `
  --resource-group $resourceGroup `
  --name $webAppName
```

## Costs
- **Basic B1 Plan**: ~$13/month
- **Custom domain SSL**: Free (App Service Managed Certificate)
- **Bandwidth**: Includes 165 GB/month
- **Azure SQL (S0)**: ~$15/month (optional)

## Quick Redeploy Script
Save for future updates:
```powershell
dotnet publish -c Release -o ./publish
Compress-Archive -Path ./publish/* -DestinationPath deploy.zip -Force
az webapp deployment source config-zip --resource-group luma-rg --name luma-laundry --src deploy.zip
Remove-Item deploy.zip
Remove-Item -Recurse ./publish
```

## Troubleshooting
```powershell
# Check app status
az webapp show --resource-group luma-rg --name luma-laundry --query state

# Restart app
az webapp restart --resource-group luma-rg --name luma-laundry

# Check logs
az webapp log tail --resource-group luma-rg --name luma-laundry
```
