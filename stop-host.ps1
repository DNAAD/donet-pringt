param(
    [int[]]$Ports = @(37121, 37122, 37123, 37124, 37125),
    [switch]$WhatIf
)

$ErrorActionPreference = 'Stop'

function Get-ListeningProcessIds {
    param([int[]]$TargetPorts)

    $ids = New-Object System.Collections.Generic.HashSet[int]
    $netstatOutput = & netstat -ano -p TCP

    foreach ($line in $netstatOutput) {
        $text = $line.Trim()
        if (-not $text.StartsWith('TCP')) {
            continue
        }

        $parts = $text -split '\s+' | Where-Object { $_ }
        if ($parts.Count -lt 5 -or $parts[3] -ne 'LISTENING') {
            continue
        }

        $localAddress = $parts[1]
        $pidText = $parts[-1]
        $portText = ($localAddress -split ':')[-1]

        $port = 0
        $ownerProcessId = 0
        if (-not [int]::TryParse($portText, [ref]$port)) {
            continue
        }

        if (-not [int]::TryParse($pidText, [ref]$ownerProcessId)) {
            continue
        }

        if ($TargetPorts -contains $port) {
            [void]$ids.Add($ownerProcessId)
        }
    }

    return $ids
}

$targetIds = New-Object System.Collections.Generic.HashSet[int]

foreach ($process in Get-Process Zytxt.PrintClient.Host -ErrorAction SilentlyContinue) {
    [void]$targetIds.Add([int]$process.Id)
}

foreach ($processId in Get-ListeningProcessIds -TargetPorts $Ports) {
    [void]$targetIds.Add([int]$processId)
}

if ($targetIds.Count -eq 0) {
    Write-Host 'No dotnet-print Host process found.'
} else {
    foreach ($processId in ($targetIds | Sort-Object)) {
        $process = Get-Process -Id $processId -ErrorAction SilentlyContinue
        if (-not $process) {
            continue
        }

        $description = "PID=$processId Name=$($process.ProcessName) Path=$($process.Path)"
        if ($WhatIf) {
            Write-Host "Would stop $description"
            continue
        }

        Write-Host "Stopping $description"
        Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
    }
}

Start-Sleep -Milliseconds 500

Write-Host 'Port status:'
foreach ($port in $Ports) {
    $stillListening = (Get-ListeningProcessIds -TargetPorts @($port)).Count -gt 0
    if ($stillListening) {
        Write-Host "${port}: LISTENING"
    } else {
        Write-Host "${port}: free"
    }
}
