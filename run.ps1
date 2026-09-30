<#
  MediLink local launcher for Windows PowerShell.
  Usage: .\run.ps1 start | stop | status
#>
param(
    [ValidateSet('start', 'stop', 'status')]
    [string]$Command = 'start'
)

$ErrorActionPreference = 'Stop'
$Root = $PSScriptRoot
$RuntimeDirectory = Join-Path $Root '.medilink-run'

if ([string]::IsNullOrWhiteSpace($env:MEDILINK_DB_USERNAME)) {
    $env:MEDILINK_DB_USERNAME = 'root'
}
if ([string]::IsNullOrWhiteSpace($env:MEDILINK_DB_PASSWORD)) {
    $env:MEDILINK_DB_PASSWORD = 'root'
}
if ([string]::IsNullOrWhiteSpace($env:MEDILINK_JWT_SECRET)) {
    $env:MEDILINK_JWT_SECRET = 'medilink-development-secret-must-be-32-characters'
}
if ([string]::IsNullOrWhiteSpace($env:MEDILINK_INTERNAL_KEY)) {
    $env:MEDILINK_INTERNAL_KEY = 'medilink-internal-development-key'
}

function Get-PidFile([string]$Name) {
    Join-Path $RuntimeDirectory "$Name.pid"
}

function Stop-MediLinkService([string]$Name) {
    $pidFile = Get-PidFile $Name
    if (-not (Test-Path $pidFile)) {
        return
    }

    $rawPid = (Get-Content -Raw $pidFile).Trim()
    $processId = 0
    if ([int]::TryParse($rawPid, [ref]$processId) -and $processId -gt 0) {
        $process = Get-Process -Id $processId -ErrorAction SilentlyContinue
        if ($null -ne $process) {
            try {
                # Stop the tracked launcher process and its children without surfacing
                # taskkill race-condition errors when the process exits on its own.
                Stop-Process -Id $processId -Force -ErrorAction Stop
                Write-Host "Stopped $Name (PID $processId)."
            } catch {
                # The process may have exited between the lookup and the stop request.
                Write-Host "Old $Name process (PID $processId) already exited; continuing."
            }
        }
    }
    Remove-Item -LiteralPath $pidFile -Force -ErrorAction SilentlyContinue
}



function Stop-PortOwner([int]$Port, [string]$Name) {
    $connections = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
    foreach ($connection in $connections) {
        $ownerId = [int]$connection.OwningProcess
        if ($ownerId -le 0) { continue }

        $owner = Get-Process -Id $ownerId -ErrorAction SilentlyContinue
        if ($null -eq $owner) { continue }

        Write-Host "Stopping $Name process $($owner.ProcessName) (PID $ownerId) using port $Port..."
        try {
            # Stop the process that actually owns the listening port. This also
            # cleans up npm/Vite, dotnet and Java children left behind by the
            # PowerShell launcher.
            Stop-Process -Id $ownerId -Force -ErrorAction SilentlyContinue
        } catch {
            # Process may exit between the lookup and Stop-Process.
        }

        # Give Windows a moment to release the socket.
        for ($i = 0; $i -lt 10; $i++) {
            Start-Sleep -Milliseconds 150
            $stillListening = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
            if (-not $stillListening) { break }
        }
    }
}

function Ensure-PortFree([int]$Port, [string]$Name) {
    $connections = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
    foreach ($connection in $connections) {
        $owner = Get-Process -Id $connection.OwningProcess -ErrorAction SilentlyContinue
        if ($null -eq $owner) { continue }

        if ($owner.ProcessName -like 'MediLink.*' -or
            (($Port -eq 5173) -and $owner.ProcessName -eq 'node') -or
            (($Port -eq 8081) -and $owner.ProcessName -eq 'java')) {
            Stop-PortOwner $Port $Name
            continue
        }

        throw "Port $Port required by $Name is already in use by $($owner.ProcessName) (PID $($owner.Id)). Stop that application and run .\run.cmd start again."
    }

    Start-Sleep -Milliseconds 300
}


function Install-CustomerWebDependencies {
    $webRoot = Join-Path $Root 'src\MediLink.Web'
    $packageJson = Join-Path $webRoot 'package.json'
    $packageLock = Join-Path $webRoot 'package-lock.json'
    $webLog = Join-Path $RuntimeDirectory 'web.log'

    if (-not (Test-Path $packageJson)) {
        throw "Customer Web package.json was not found at $webRoot."
    }

    Write-Host "Installing/verifying Customer Web npm dependencies..."
    Push-Location $webRoot
    try {
        # npm install is intentionally run on every 'start' so a fresh clone,
        # changed package.json, or missing node_modules is repaired automatically.
        & npm.cmd install --no-audit --no-fund *>> $webLog
        if ($LASTEXITCODE -ne 0) {
            throw "npm install failed for Customer Web. Check $webLog."
        }
    } finally {
        Pop-Location
    }
    Write-Host "Customer Web npm dependencies are ready."
}

function Start-MediLinkService([string]$Name, [string]$Script) {
    $pidFile = Get-PidFile $Name
    $logFile = Join-Path $RuntimeDirectory "$Name.log"
    if (Test-Path $pidFile) {
        $existingId = Get-Content -Raw $pidFile
        if (Get-Process -Id $existingId -ErrorAction SilentlyContinue) {
            Write-Host "$Name is already running (PID $existingId)."
            return
        }
        Remove-Item -LiteralPath $pidFile -Force
    }

    $encodedScript = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($Script))
    $process = Start-Process -FilePath 'powershell.exe' -ArgumentList '-NoProfile', '-EncodedCommand', $encodedScript -WindowStyle Hidden -PassThru
    Set-Content -LiteralPath $pidFile -Value $process.Id
    Write-Host "Started $Name (log: $logFile)."
}

function Wait-ForHttp([string]$Url, [string]$Name) {
    $deadline = (Get-Date).AddSeconds(180)
    do {
        Start-Sleep -Seconds 2
        try {
            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 5
            if ($response.StatusCode -eq 200) { Write-Host "$Name is healthy."; return }
        } catch { }
        if ((Get-Date) -gt $deadline) {
            $log = Join-Path $RuntimeDirectory ((($Name -replace ' ', '-') + '.log').ToLower())
            Write-Host "$Name failed to become healthy. Check $RuntimeDirectory for logs." -ForegroundColor Red
            throw "$Name did not become healthy."
        }
    } while ($true)
}

switch ($Command) {
    'start' {
        if ([string]::IsNullOrWhiteSpace($env:MEDILINK_DB_PASSWORD)) {
            throw 'Set MEDILINK_DB_PASSWORD first. Example: $env:MEDILINK_DB_PASSWORD = ''root'''
        }
        if ([string]::IsNullOrWhiteSpace($env:MEDILINK_JWT_SECRET) -or $env:MEDILINK_JWT_SECRET.Length -lt 32) {
            throw 'Set MEDILINK_JWT_SECRET first. It must contain at least 32 characters.'
        }

        if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'dotnet SDK is required. Install the .NET 10 SDK and reopen PowerShell.' }
        if (-not (Get-Command npm.cmd -ErrorAction SilentlyContinue)) { throw 'npm is required. Install Node.js and reopen PowerShell.' }
        if (-not (Get-Command mvn.cmd -ErrorAction SilentlyContinue)) { throw 'Maven (mvn.cmd) is required for the Java Store Portal.' }
        if (-not (Get-Command java -ErrorAction SilentlyContinue)) { throw 'Java is required for the Store Portal.' }

        $javaVersion = (& java --version 2>$null | Out-String).Trim()
        if ($javaVersion -notmatch '(?m)^java 25(?:\.|$)') { throw "Java 25 is required for this project. Detected: $javaVersion" }

        New-Item -ItemType Directory -Path $RuntimeDirectory -Force | Out-Null
        'api', 'auth', 'inventory', 'order', 'web', 'store-portal' | ForEach-Object { Stop-MediLinkService $_ }
        Get-ChildItem -LiteralPath $RuntimeDirectory -Filter '*.log' -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
        $dbUser = if ([string]::IsNullOrWhiteSpace($env:MEDILINK_DB_USERNAME)) { 'root' } else { $env:MEDILINK_DB_USERNAME }
        $dbHost = if ([string]::IsNullOrWhiteSpace($env:MEDILINK_DB_HOST)) { 'localhost' } else { $env:MEDILINK_DB_HOST }
        $dbPort = if ([string]::IsNullOrWhiteSpace($env:MEDILINK_DB_PORT)) { '3306' } else { $env:MEDILINK_DB_PORT }
        $authDb = if ([string]::IsNullOrWhiteSpace($env:MEDILINK_AUTH_DB_NAME)) { 'MediLinkAuth' } else { $env:MEDILINK_AUTH_DB_NAME }
        $orderDb = if ([string]::IsNullOrWhiteSpace($env:MEDILINK_ORDER_DB_NAME)) { 'MediLinkOrder' } else { $env:MEDILINK_ORDER_DB_NAME }
        $authUser = if ([string]::IsNullOrWhiteSpace($env:MEDILINK_AUTH_DB_USERNAME)) { $dbUser } else { $env:MEDILINK_AUTH_DB_USERNAME }
        $orderUser = if ([string]::IsNullOrWhiteSpace($env:MEDILINK_ORDER_DB_USERNAME)) { $dbUser } else { $env:MEDILINK_ORDER_DB_USERNAME }
        $authPassword = if ($env:MEDILINK_AUTH_DB_PASSWORD) { $env:MEDILINK_AUTH_DB_PASSWORD } else { $env:MEDILINK_DB_PASSWORD }
        $orderPassword = if ($env:MEDILINK_ORDER_DB_PASSWORD) { $env:MEDILINK_ORDER_DB_PASSWORD } else { $env:MEDILINK_DB_PASSWORD }
        if (-not (Test-NetConnection -ComputerName $dbHost -Port ([int]$dbPort) -InformationLevel Quiet)) {
            throw "MySQL is not reachable at $dbHost`:$dbPort. Start MySQL first, then run .\run.cmd start again."
        }
        $authConnection = "Server=$dbHost;Port=$dbPort;Database=$authDb;User ID=$authUser;Password=$authPassword;"
        $orderConnection = "Server=$dbHost;Port=$dbPort;Database=$orderDb;User ID=$orderUser;Password=$orderPassword;"


        Ensure-PortFree 5101 'Auth'
        Start-MediLinkService 'auth' "`$env:MEDILINK_AUTO_CREATE_DATABASE = 'true'; `$env:ConnectionStrings__AuthConnection = '$authConnection'; `$env:InventoryService__BaseUrl = 'http://localhost:5201/'; `$env:JwtSettings__Secret = '$($env:MEDILINK_JWT_SECRET)'; `$env:ASPNETCORE_ENVIRONMENT = 'Development'; `$env:ASPNETCORE_URLS = 'http://localhost:5101'; Set-Location '$Root'; dotnet run --project src\MediLink.Auth --no-launch-profile *>> '$(Join-Path $RuntimeDirectory 'auth.log')'"
        Wait-ForHttp 'http://localhost:5101/ready' 'Auth'

        Ensure-PortFree 5201 'Inventory'
        Start-MediLinkService 'inventory' "`$env:JwtSettings__Secret = '$($env:MEDILINK_JWT_SECRET)'; `$env:ASPNETCORE_ENVIRONMENT = 'Development'; `$env:MEDILINK_INTERNAL_KEY = '$($env:MEDILINK_INTERNAL_KEY)'; `$env:ASPNETCORE_URLS = 'http://localhost:5201'; Set-Location '$Root'; dotnet run --project src\MediLink.Inventory --no-launch-profile *>> '$(Join-Path $RuntimeDirectory 'inventory.log')'"
        Wait-ForHttp 'http://localhost:5201/ready' 'Inventory'

        Ensure-PortFree 5301 'Order'
        Start-MediLinkService 'order' "`$env:MEDILINK_AUTO_CREATE_DATABASE = 'true'; `$env:ConnectionStrings__OrderConnection = '$orderConnection'; `$env:JwtSettings__Secret = '$($env:MEDILINK_JWT_SECRET)'; `$env:ASPNETCORE_ENVIRONMENT = 'Development'; `$env:MEDILINK_INTERNAL_KEY = '$($env:MEDILINK_INTERNAL_KEY)'; `$env:ASPNETCORE_URLS = 'http://localhost:5301'; Set-Location '$Root'; dotnet run --project src\MediLink.Order --no-launch-profile *>> '$(Join-Path $RuntimeDirectory 'order.log')'"
        Wait-ForHttp 'http://localhost:5301/ready' 'Order'

        Ensure-PortFree 5140 'API Gateway'
        Start-MediLinkService 'api' "`$env:JwtSettings__Secret = '$($env:MEDILINK_JWT_SECRET)'; `$env:ASPNETCORE_ENVIRONMENT = 'Development'; `$env:ASPNETCORE_URLS = 'http://localhost:5140'; Set-Location '$Root'; dotnet run --project src\MediLink.Api --no-launch-profile *>> '$(Join-Path $RuntimeDirectory 'api.log')'"
        Wait-ForHttp 'http://localhost:5140/health' 'API Gateway'

        Ensure-PortFree 5173 'Customer Web'
        Install-CustomerWebDependencies
        Start-MediLinkService 'web' "Set-Location '$(Join-Path $Root 'src\MediLink.Web')'; npm.cmd run dev -- --host 127.0.0.1 *>> '$(Join-Path $RuntimeDirectory 'web.log')'"
        Ensure-PortFree 8081 'Store Portal'
        Start-MediLinkService 'store-portal' "`$env:MEDILINK_INTERNAL_KEY = '$($env:MEDILINK_INTERNAL_KEY)'; Set-Location '$(Join-Path $Root 'src\MediLink.Store.Java')'; mvn.cmd spring-boot:run *>> '$(Join-Path $RuntimeDirectory 'store-portal.log')'"
        Wait-ForHttp 'http://localhost:8081/login' 'Store Portal'

        Write-Host "`nMediLink is starting:"
        Write-Host '  Customer web:        http://localhost:5173'
        Write-Host '  Customer login:      http://localhost:5173/login'
        Write-Host '  Admin login:         http://localhost:5173/admin-login'
        Write-Host '  API / Swagger:       http://localhost:5140/swagger'
        Write-Host '  API health:          http://localhost:5140/health'
        Write-Host '  Auth service:        http://localhost:5101/health'
        Write-Host '  Inventory service:   http://localhost:5201/health'
        Write-Host '  Order service:       http://localhost:5301/health'
        Write-Host '  Store portal:        http://localhost:8081/login'
    }
    'stop' {
        # Stop the tracked launcher processes first.
        'api', 'auth', 'inventory', 'order', 'web', 'store-portal' | ForEach-Object { Stop-MediLinkService $_ }

        # Then clean up the actual listeners. npm/Vite, dotnet and Java can
        # outlive their PowerShell wrapper, so PID files alone are not enough.
        $ports = @(
            @{ Port = 5101; Name = 'Auth' },
            @{ Port = 5201; Name = 'Inventory' },
            @{ Port = 5301; Name = 'Order' },
            @{ Port = 5140; Name = 'API Gateway' },
            @{ Port = 5173; Name = 'Customer Web' },
            @{ Port = 8081; Name = 'Store Portal' }
        )
        foreach ($entry in $ports) {
            Stop-PortOwner $entry.Port $entry.Name
        }

        Write-Host 'All MediLink service ports have been released.'
    }
    'status' {
        'api', 'auth', 'inventory', 'order', 'web', 'store-portal' | ForEach-Object {
            $pidFile = Get-PidFile $_
            $processId = if (Test-Path $pidFile) { Get-Content -Raw $pidFile } else { $null }
            $state = if ($processId -and (Get-Process -Id $processId -ErrorAction SilentlyContinue)) { "running (PID $processId)" } else { 'stopped' }
            Write-Host "$_`: $state"
        }
    }
}
