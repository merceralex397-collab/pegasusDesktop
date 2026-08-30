<#
.SYNOPSIS
Exports the gated Pegasus Desktop Gateway OpenAPI document.

.DESCRIPTION
The API tooling lives under eng/api/ so this exporter and the Kiota generator
owned by the next gateway ticket form one API-tooling unit. The repository's
PowerShell scripts remain under scripts/ for repository-wide operations.

The command starts the configured Pegasus.Web host, requests its normalised
OpenAPI document, and writes openapi/pegasus-v1.json. Regenerate the committed
snapshot with:

    pwsh ./eng/api/Export-OpenApiDocument.ps1

The command exits non-zero if the host cannot start or the document cannot be
retrieved.
#>

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$projectPath = Join-Path $repositoryRoot 'src/Pegasus.Web/Pegasus.Web.csproj'
$snapshotPath = Join-Path $repositoryRoot 'openapi/pegasus-v1.json'
$listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
$listener.Start()
$port = ([Net.IPEndPoint]$listener.LocalEndpoint).Port
$listener.Stop()
$uri = "http://127.0.0.1:$port"

function ConvertTo-StableValue {
    param(
        [Parameter(Mandatory)] [AllowNull()] $Value,
        [switch] $Root
    )

    if ($null -eq $Value) {
        return $null
    }

    if ($Value -is [System.Collections.IDictionary]) {
        $stable = [ordered]@{}
        $keys = [System.Collections.Generic.List[string]]::new()
        foreach ($key in $Value.Keys) {
            $keys.Add([string]$key)
        }

        $keys.Sort([StringComparer]::Ordinal)
        foreach ($key in $keys) {
            if ($Root -and $key -eq 'servers') {
                continue
            }

            $stable[$key] = ConvertTo-StableValue -Value $Value[$key]
        }

        return $stable
    }

    if ($Value -is [System.Collections.IList]) {
        $stable = [System.Collections.Generic.List[object]]::new()
        foreach ($item in $Value) {
            $stable.Add((ConvertTo-StableValue -Value $item))
        }

        # PowerShell enumerates function output by default. Preserve an
        # OpenAPI array with one item as an array rather than collapsing it to
        # a scalar when assigning the normalized value into its parent object.
        return ,$stable
    }

    return $Value
}

function ConvertTo-NormalizedJson {
    param([Parameter(Mandatory)] [string] $Json)

    $parsed = ConvertFrom-Json -InputObject $Json -AsHashtable
    $stable = ConvertTo-StableValue -Value $parsed -Root
    $options = [System.Text.Json.JsonSerializerOptions]::new()
    $options.WriteIndented = $true
    return ([System.Text.Json.JsonSerializer]::Serialize($stable, $options) -replace "`r`n", "`n") + "`n"
}

$startInfo = [Diagnostics.ProcessStartInfo]::new()
$startInfo.FileName = 'dotnet'
$startInfo.WorkingDirectory = $repositoryRoot
$startInfo.RedirectStandardOutput = $true
$startInfo.RedirectStandardError = $true
$startInfo.UseShellExecute = $false
foreach ($argument in @(
        'run', '--project', $projectPath, '--configuration', 'Release',
        '--no-build', '--no-restore', '--no-launch-profile', '--urls', $uri)) {
    $startInfo.ArgumentList.Add($argument)
}
$startInfo.Environment['ASPNETCORE_ENVIRONMENT'] = 'Development'
$startInfo.Environment['ASPNETCORE_URLS'] = $uri
$startInfo.Environment['Runtime__Profile'] = 'DevelopmentOffline'
$startInfo.Environment['Features__DesktopGateway'] = 'true'
$process = [Diagnostics.Process]::new()
$process.StartInfo = $startInfo

try {
    if (-not $process.Start()) {
        throw 'The Pegasus.Web process could not be started.'
    }

    $response = $null
    for ($attempt = 0; $attempt -lt 80 -and $null -eq $response; $attempt++) {
        if ($process.HasExited) {
            $stdout = $process.StandardOutput.ReadToEnd()
            $stderr = $process.StandardError.ReadToEnd()
            throw "Pegasus.Web exited before serving OpenAPI. stdout: $stdout stderr: $stderr"
        }

        try {
            $response = Invoke-WebRequest -Uri "$uri/openapi/v1.json" -UseBasicParsing
        }
        catch {
            Start-Sleep -Milliseconds 250
        }
    }

    if ($null -eq $response) {
        throw "Timed out waiting for Pegasus.Web to serve $uri/openapi/v1.json."
    }

    $normalized = ConvertTo-NormalizedJson -Json $response.Content
    $snapshotDirectory = Split-Path -Parent $snapshotPath
    [IO.Directory]::CreateDirectory($snapshotDirectory) | Out-Null
    [IO.File]::WriteAllText($snapshotPath, $normalized, [Text.UTF8Encoding]::new($false))
}
finally {
    if ($null -ne $process -and -not $process.HasExited) {
        $process.Kill($true)
        $process.WaitForExit()
    }

    $process.Dispose()
}
