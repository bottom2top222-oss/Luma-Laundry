# LaundryApp

ASP.NET Core MVC app for Luma Laundry with layered API + worker support.

## Configuration Reference

Use double underscore (`__`) in environment variables to map nested config keys.

### Core Runtime

- `ASPNETCORE_ENVIRONMENT`
  - Typical values: `Development`, `Production`
- `Database__Path`
  - Local Windows example: `laundry.db`
  - Railway/Linux example: `/var/data/laundry.db`
  - Azure Linux App Service example: `/home/site/wwwroot/App_Data/laundry.db`

### Layered Services

- `LayeredServices__ApiBaseUrl`
  - Default local value: `http://localhost:5080`
- `LayeredServices__ApiOnlyMode`
  - `false` in local/default mode (allows fallback)
  - `true` in production API-only mode

### Maintenance Mode

- `MAINTENANCE_MODE`
  - `true` returns maintenance page with HTTP 503
  - `false` (or unset) runs normally

### Email Settings

- `Email__SmtpHost`
- `Email__SmtpPort`
- `Email__EnableSsl`
- `Email__Username`
- `Email__Password`
- `Email__FromAddress`
- `Email__FromName`

### Stripe Settings

- `STRIPE_PUBLISHABLE_KEY`
- `STRIPE_SECRET_KEY`
- `STRIPE_WEBHOOK_SECRET`

Notes:
- Use Stripe dashboard values directly (single line, no extra quotes).
- Use test keys in non-production and live keys in production.
- `STRIPE_WEBHOOK_SECRET` must match the endpoint configured for `POST /api/stripe/webhook`.

### Seeded Admin (override defaults)

- `DefaultAdmin__Email`
- `DefaultAdmin__Password`

## Deployment Notes

- Railway guide: `RAILWAY_DEPLOY.md`
- Azure guide: `AZURE_DEPLOY.md`

## Railway Multi-Service Containers

Use these Dockerfiles when creating separate Railway services from the same repo:

- `docker/Dockerfile.frontend` → MVC/frontend app
- `docker/Dockerfile.api` → API service
- `docker/Dockerfile.worker` → background worker

For exact Railway UI setup steps, use the **Railway Click-Path Checklist (3-Service Setup)** section in `RAILWAY_DEPLOY.md`.

## Run Matrix

| Mode | API Running? | `ASPNETCORE_ENVIRONMENT` | `LayeredServices__ApiOnlyMode` | `Database__Path` | Expected Behavior |
|---|---|---|---|---|---|
| Local fallback (default) | Optional | `Development` | `false` | `laundry.db` | App works; local fallback allowed if API is down |
| Local API-only (strict) | Yes | `Production` | `true` | `laundry.db` | App depends on API for order/admin operations |
| Railway production | Yes | `Production` | `true` | `/var/data/laundry.db` | API-only behavior with persistent volume |
| Azure Linux App Service | Yes | `Production` | `true` | `/home/site/wwwroot/App_Data/laundry.db` | API-only behavior using App Service storage |

### Copy/Paste Run Commands

#### Local fallback mode (API optional)

```powershell
$env:ASPNETCORE_ENVIRONMENT='Development'
$env:LayeredServices__ApiOnlyMode='false'
$env:Database__Path='laundry.db'
dotnet run --project LaundryApp.csproj --urls http://localhost:5147
```

#### Local API-only mode (requires API up)

```powershell
# Terminal 1: API
dotnet run --project services/LaundryApp.Api/LaundryApp.Api.csproj --urls http://localhost:5080

# Terminal 2: MVC app
$env:ASPNETCORE_ENVIRONMENT='Production'
$env:LayeredServices__ApiOnlyMode='true'
$env:Database__Path='laundry.db'
dotnet run --no-launch-profile --project LaundryApp.csproj --urls http://localhost:5147
```

## Quick Local Run

```powershell
dotnet build LaundryApp.sln
dotnet run --project LaundryApp.csproj --urls http://localhost:5147
```

## Common Startup Failures

### 1) App exits immediately or port appears busy

Kill stale processes, then start again:

```powershell
taskkill /F /IM dotnet.exe
taskkill /F /IM LaundryApp.exe
dotnet run --project LaundryApp.csproj --urls http://localhost:5147
```

### 2) Production run behaves like Development

Launch profiles can override environment settings. Use no-launch-profile for strict production behavior:

```powershell
$env:ASPNETCORE_ENVIRONMENT='Production'
dotnet run --no-launch-profile --project LaundryApp.csproj --urls http://localhost:5147
```

### 3) API-only mode test with API down

Run MVC in API-only mode and leave API offline to verify outage behavior:

```powershell
$env:ASPNETCORE_ENVIRONMENT='Production'
$env:LayeredServices__ApiOnlyMode='true'
$env:Database__Path='laundry.db'
dotnet run --no-launch-profile --project LaundryApp.csproj --urls http://localhost:5147
```

Expected result: site starts, but order/admin write operations that depend on API are blocked.

### 4) One-command health check (MVC + API)

Run this in PowerShell after startup:

```powershell
$checks = @(
  @{ Name = 'MVC Home'; Url = 'http://localhost:5147/' },
  @{ Name = 'API Admin Orders'; Url = 'http://localhost:5080/api/admin/orders' }
)

foreach ($check in $checks) {
  try {
    $response = Invoke-WebRequest -Uri $check.Url -UseBasicParsing
    Write-Output ("OK   [{0}] {1} -> {2}" -f $response.StatusCode, $check.Name, $check.Url)
  }
  catch {
    $status = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 'DOWN' }
    Write-Output ("FAIL [{0}] {1} -> {2}" -f $status, $check.Name, $check.Url)
  }
}
```
