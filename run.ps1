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

function Get-PidFile([string]$Name) {
    Join-Path $RuntimeDirectory "$Name.pid"
}

function Stop-MediLinkService([string]$Name) {
    $pidFile = Get-PidFile $Name
    if (-not (Test-Path $pidFile)) {
        return
    }

    $processId = Get-Content -Raw $pidFile
    if (Get-Process -Id $processId -ErrorAction SilentlyContinue) {
        # /T also stops child processes such as dotnet, npm, and Maven.
        & taskkill.exe /PID $processId /T /F | Out-Null
        Write-Host "Stopped $Name."
    }
    Remove-Item -LiteralPath $pidFile -Force -ErrorAction SilentlyContinue
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

switch ($Command) {
    'start' {
        if ([string]::IsNullOrWhiteSpace($env:MEDILINK_DB_PASSWORD)) {
            throw 'Set MEDILINK_DB_PASSWORD first. Example: $env:MEDILINK_DB_PASSWORD = ''root'''
        }
        if ([string]::IsNullOrWhiteSpace($env:MEDILINK_JWT_SECRET) -or $env:MEDILINK_JWT_SECRET.Length -lt 32) {
            throw 'Set MEDILINK_JWT_SECRET first. It must contain at least 32 characters.'
        }

        New-Item -ItemType Directory -Path $RuntimeDirectory -Force | Out-Null
        $dbUser = if ([string]::IsNullOrWhiteSpace($env:MEDILINK_DB_USERNAME)) { 'root' } else { $env:MEDILINK_DB_USERNAME }
        $dbHost = if ([string]::IsNullOrWhiteSpace($env:MEDILINK_DB_HOST)) { 'localhost' } else { $env:MEDILINK_DB_HOST }
        $dbPort = if ([string]::IsNullOrWhiteSpace($env:MEDILINK_DB_PORT)) { '3306' } else { $env:MEDILINK_DB_PORT }
        $dbName = if ([string]::IsNullOrWhiteSpace($env:MEDILINK_DB_NAME)) { 'MediLink' } else { $env:MEDILINK_DB_NAME }
        $javaDbUrl = if ([string]::IsNullOrWhiteSpace($env:MEDILINK_DB_URL)) { "jdbc:mysql://${dbHost}:${dbPort}/${dbName}?useSSL=false&serverTimezone=UTC" } else { $env:MEDILINK_DB_URL }
        $connectionString = "Server=$dbHost;Port=$dbPort;Database=$dbName;User ID=$dbUser;Password=$($env:MEDILINK_DB_PASSWORD);"

        Start-MediLinkService 'api' "`$env:ConnectionStrings__DefaultConnection = '$connectionString'; `$env:JwtSettings__Secret = '$($env:MEDILINK_JWT_SECRET)'; Set-Location '$Root'; dotnet run --project src\MediLink.Api *>> '$(Join-Path $RuntimeDirectory 'api.log')'"
        Start-MediLinkService 'web' "Set-Location '$(Join-Path $Root 'src\MediLink.Web')'; npm.cmd run dev -- --host 127.0.0.1 *>> '$(Join-Path $RuntimeDirectory 'web.log')'"
        Start-MediLinkService 'store-portal' "`$env:MEDILINK_DB_URL = '$javaDbUrl'; `$env:MEDILINK_DB_USERNAME = '$dbUser'; `$env:MEDILINK_DB_PASSWORD = '$($env:MEDILINK_DB_PASSWORD)'; Set-Location '$(Join-Path $Root 'src\MediLink.Store.Java')'; mvn.cmd spring-boot:run *>> '$(Join-Path $RuntimeDirectory 'store-portal.log')'"

        Write-Host "`nMediLink is starting:"
        Write-Host '  Customer web:        http://localhost:5173'
        Write-Host '  Customer login:      http://localhost:5173/login'
        Write-Host '  Admin login:         http://localhost:5173/admin-login'
        Write-Host '  API / Swagger:       http://localhost:5140/swagger'
        Write-Host '  API health:          http://localhost:5140/health'
        Write-Host '  Store portal:        http://localhost:8081/login'
    }
    'stop' { 'api', 'web', 'store-portal' | ForEach-Object { Stop-MediLinkService $_ } }
    'status' {
        'api', 'web', 'store-portal' | ForEach-Object {
            $pidFile = Get-PidFile $_
            $processId = if (Test-Path $pidFile) { Get-Content -Raw $pidFile } else { $null }
            $state = if ($processId -and (Get-Process -Id $processId -ErrorAction SilentlyContinue)) { "running (PID $processId)" } else { 'stopped' }
            Write-Host "$_`: $state"
        }
    }
}
