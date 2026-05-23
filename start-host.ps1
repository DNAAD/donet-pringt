param(
    [switch]$Build,
    [switch]$NoStop
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$workspace = Split-Path -Parent $root
$dotnet = Join-Path $workspace '.dotnet\dotnet.exe'
$solution = Join-Path $root 'zytxt-dotnet-print.sln'
$hostDll = Join-Path $root 'src\Zytxt.PrintClient.Host\bin\Debug\net8.0-windows\Zytxt.PrintClient.Host.dll'
$dataDir = Join-Path $root '.runtime-preview'
$url = 'http://127.0.0.1:37122'

if (-not (Test-Path $dotnet)) {
    throw "dotnet.exe not found: $dotnet"
}

if (-not $NoStop) {
    $connection = Get-NetTCPConnection -LocalPort 37122 -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($connection) {
        Write-Host "Stopping existing Host process $($connection.OwningProcess) on 37122..."
        Stop-Process -Id $connection.OwningProcess -Force
        Start-Sleep -Seconds 1
    }
}

$currentPath = [System.Environment]::GetEnvironmentVariable('Path', 'Process')
if ([string]::IsNullOrEmpty($currentPath)) {
    $currentPath = [System.Environment]::GetEnvironmentVariable('PATH', 'Process')
}

[System.Environment]::SetEnvironmentVariable('PATH', $null, 'Process')
[System.Environment]::SetEnvironmentVariable('Path', "$workspace\.dotnet;$currentPath", 'Process')
[System.Environment]::SetEnvironmentVariable('DOTNET_CLI_HOME', (Join-Path $workspace '.dotnet_home'), 'Process')
[System.Environment]::SetEnvironmentVariable('DOTNET_SKIP_FIRST_TIME_EXPERIENCE', '1', 'Process')
[System.Environment]::SetEnvironmentVariable('APPDATA', (Join-Path $workspace '.dotnet_appdata'), 'Process')
[System.Environment]::SetEnvironmentVariable('LOCALAPPDATA', (Join-Path $workspace '.dotnet_localappdata'), 'Process')
[System.Environment]::SetEnvironmentVariable('NUGET_PACKAGES', (Join-Path $workspace '.nuget_packages'), 'Process')
[System.Environment]::SetEnvironmentVariable('ZYTXT_PRINT_URL', $url, 'Process')
[System.Environment]::SetEnvironmentVariable('ZYTXT_PRINT_DATA_DIR', $dataDir, 'Process')

if ($Build) {
    Write-Host "Building $solution..."
    & $dotnet build $solution
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

if (-not (Test-Path $hostDll)) {
    throw "Host dll not found: $hostDll. Run with -Build first."
}

Write-Host "Starting Host at $url..."
$runner = Join-Path $root 'run-host.ps1'
Start-Process -FilePath 'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe' `
    -ArgumentList @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $runner, '-Url', $url, '-DataDir', $dataDir) `
    -WorkingDirectory $workspace `
    -WindowStyle Hidden

Start-Sleep -Seconds 3

$listener = Get-NetTCPConnection -LocalPort 37122 -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $listener) {
    throw "Host did not start listening on 37122."
}

Write-Host "Host started. PID: $($listener.OwningProcess)"
Write-Host "Settings UI: $url/settings-ui"
