# Azure App Service Deployment Script for Luma
# Run this script to deploy your app to Azure

param(
    [string]$ResourceGroup = "luma-rg",
    [string]$Location = "eastus",
    [string]$AppServicePlan = "luma-plan",
    [string]$WebAppName = "luma-laundry",
    [switch]$CreateResources,
    [switch]$Deploy
)

Write-Host "=== Luma Azure Deployment ===" -ForegroundColor Cyan

# Check if Azure CLI is installed
try {
    az version | Out-Null
} catch {
    Write-Host "Azure CLI not found. Please install it:" -ForegroundColor Red
    Write-Host "winget install Microsoft.AzureCLI" -ForegroundColor Yellow
    exit 1
}

# Check if logged in
Write-Host "`nChecking Azure login status..." -ForegroundColor Cyan
$account = az account show 2>$null
if (!$account) {
    Write-Host "Not logged in to Azure. Running 'az login'..." -ForegroundColor Yellow
    az login
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Login failed!" -ForegroundColor Red
        exit 1
    }
}

Write-Host "Logged in successfully!" -ForegroundColor Green

# Create resources if requested
if ($CreateResources) {
    Write-Host "`n=== Creating Azure Resources ===" -ForegroundColor Cyan
    
    # Create resource group
    Write-Host "Creating resource group: $ResourceGroup..." -ForegroundColor Yellow
    az group create --name $ResourceGroup --location $Location
    
    # Create App Service plan
    Write-Host "Creating App Service plan: $AppServicePlan (B1 tier)..." -ForegroundColor Yellow
    az appservice plan create `
        --name $AppServicePlan `
        --resource-group $ResourceGroup `
        --sku B1 `
        --is-linux
    
    # Create web app
    Write-Host "Creating web app: $WebAppName..." -ForegroundColor Yellow
    az webapp create `
        --resource-group $ResourceGroup `
        --plan $AppServicePlan `
        --name $WebAppName `
        --runtime "DOTNET:8.0"
    
    # Configure app settings
    Write-Host "Configuring app settings..." -ForegroundColor Yellow
    az webapp config appsettings set `
        --resource-group $ResourceGroup `
        --name $WebAppName `
        --settings ASPNETCORE_ENVIRONMENT=Production
    
    Write-Host "`nResources created successfully!" -ForegroundColor Green
    
    # Get app URL
    $appUrl = az webapp show --resource-group $ResourceGroup --name $WebAppName --query defaultHostName --output tsv
    Write-Host "App URL: https://$appUrl" -ForegroundColor Cyan
}

# Deploy if requested
if ($Deploy) {
    Write-Host "`n=== Deploying Application ===" -ForegroundColor Cyan
    
    # Publish the app
    Write-Host "Publishing application..." -ForegroundColor Yellow
    dotnet publish -c Release -o ./publish
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Publish failed!" -ForegroundColor Red
        exit 1
    }
    
    # Create deployment package
    Write-Host "Creating deployment package..." -ForegroundColor Yellow
    if (Test-Path deploy.zip) {
        Remove-Item deploy.zip
    }
    Compress-Archive -Path ./publish/* -DestinationPath deploy.zip -Force
    
    # Deploy to Azure
    Write-Host "Deploying to Azure App Service..." -ForegroundColor Yellow
    az webapp deployment source config-zip `
        --resource-group $ResourceGroup `
        --name $WebAppName `
        --src deploy.zip
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Deployment failed!" -ForegroundColor Red
        exit 1
    }
    
    # Clean up
    Write-Host "Cleaning up..." -ForegroundColor Yellow
    Remove-Item deploy.zip
    Remove-Item -Recurse ./publish
    
    Write-Host "`nDeployment completed successfully!" -ForegroundColor Green
    
    # Get app URL
    $appUrl = az webapp show --resource-group $ResourceGroup --name $WebAppName --query defaultHostName --output tsv
    Write-Host "App URL: https://$appUrl" -ForegroundColor Cyan
}

# Show help if no parameters
if (!$CreateResources -and !$Deploy) {
    Write-Host "`nUsage:" -ForegroundColor Cyan
    Write-Host "  .\deploy-azure.ps1 -CreateResources    # Create Azure resources"
    Write-Host "  .\deploy-azure.ps1 -Deploy             # Deploy application"
    Write-Host "  .\deploy-azure.ps1 -CreateResources -Deploy  # Both"
    Write-Host ""
    Write-Host "First time setup:" -ForegroundColor Yellow
    Write-Host "  1. Run: .\deploy-azure.ps1 -CreateResources -Deploy"
    Write-Host "  2. Configure custom domain in Azure Portal"
    Write-Host "  3. For updates, run: .\deploy-azure.ps1 -Deploy"
}

Write-Host ""
