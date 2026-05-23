param(
    [string]$Url = 'http://127.0.0.1:37122',
    [string]$DataDir = ''
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$workspace = Split-Path -Parent $root
$dotnet = Join-Path $workspace '.dotnet\dotnet.exe'
$hostDll = Join-Path $root 'src\Zytxt.PrintClient.Host\bin\Debug\net8.0-windows\Zytxt.PrintClient.Host.dll'

if ([string]::IsNullOrWhiteSpace($DataDir)) {
    $DataDir = Join-Path $root '.runtime-preview'
}

$env:PATH = "$workspace\.dotnet;$env:PATH"
$env:DOTNET_CLI_HOME = Join-Path $workspace '.dotnet_home'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:APPDATA = Join-Path $workspace '.dotnet_appdata'
$env:LOCALAPPDATA = Join-Path $workspace '.dotnet_localappdata'
$env:NUGET_PACKAGES = Join-Path $workspace '.nuget_packages'
$env:ZYTXT_PRINT_URL = $Url
$env:ZYTXT_PRINT_DATA_DIR = $DataDir

Set-Location $workspace
& $dotnet $hostDll
