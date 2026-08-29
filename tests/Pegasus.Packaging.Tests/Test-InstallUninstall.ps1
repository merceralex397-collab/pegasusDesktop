[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateScript({
        if (-not (Test-Path -LiteralPath $_ -PathType Leaf)) {
            throw "Package does not exist: $_"
        }

        if ([IO.Path]::GetExtension($_) -ne '.msix') {
            throw "Package must be an .msix file: $_"
        }

        $true
    })]
    [string] $Package,

    [string] $DpapiStorePath,

    [string] $PackageFamilyRoot = (Join-Path $env:LOCALAPPDATA 'Packages'),

    [string] $LaunchInputFolder,

    [string] $ResultLogPath = (Join-Path $env:TEMP ("Pegasus-Packaging-{0:yyyyMMdd-HHmmss}.log" -f (Get-Date)))
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$expectedPackageName = 'CollisionEngineers.Pegasus'
$expectedPublisher = 'CN=Collision Engineers'
$packagePath = (Resolve-Path -LiteralPath $Package).Path
$packageFamilyRootPath = [IO.Path]::GetFullPath($PackageFamilyRoot)
$resultLog = [IO.Path]::GetFullPath($ResultLogPath)
$installedPackageFullName = $null
$installedPackageFamilyName = $null
$launchProcessId = $null
$scriptFailed = $false

$resultDirectory = Split-Path -Parent $resultLog
if ($resultDirectory -and -not (Test-Path -LiteralPath $resultDirectory -PathType Container)) {
    New-Item -ItemType Directory -Path $resultDirectory | Out-Null
}

function Format-Actual {
    param([AllowNull()][object] $Value)

    if ($null -eq $Value) {
        return '<null>'
    }

    if ($Value -is [System.Array]) {
        if ($Value.Count -eq 0) {
            return '<none>'
        }

        return (($Value | ForEach-Object { "$_" }) -join ', ')
    }

    return "$Value"
}

function Write-Result {
    param(
        [Parameter(Mandatory)] [string] $Command,
        [Parameter(Mandatory)] [object] $Expected,
        [Parameter(Mandatory)] [object] $Actual,
        [ValidateSet('PASS', 'FAIL', 'INFO')] [string] $Result = 'INFO'
    )

    $line = "{0:u}`t{1}`tcommand={2}`texpected={3}`tactual={4}" -f @(
        (Get-Date),
        $Result,
        $Command,
        (Format-Actual $Expected),
        (Format-Actual $Actual)
    )
    Add-Content -LiteralPath $resultLog -Value $line -Encoding utf8
    Write-Host $line
}

function Assert-Equal {
    param(
        [Parameter(Mandatory)] [string] $Command,
        [Parameter(Mandatory)] [object] $Expected,
        [AllowNull()] [object] $Actual
    )

    $matches = if ($null -eq $Expected) {
        $null -eq $Actual
    } else {
        "$Expected" -ceq "$Actual"
    }

    if (-not $matches) {
        Write-Result -Command $Command -Expected $Expected -Actual $Actual -Result FAIL
        throw "Assertion failed for $Command. Expected '$Expected'; actual '$(Format-Actual $Actual)'."
    }

    Write-Result -Command $Command -Expected $Expected -Actual $Actual -Result PASS
}

function Assert-True {
    param(
        [Parameter(Mandatory)] [string] $Command,
        [Parameter(Mandatory)] [bool] $Condition,
        [Parameter(Mandatory)] [object] $Actual
    )

    if (-not $Condition) {
        Write-Result -Command $Command -Expected $true -Actual $Actual -Result FAIL
        throw "Assertion failed for $Command. Expected 'True'; actual '$(Format-Actual $Actual)'."
    }

    Write-Result -Command $Command -Expected $true -Actual $Actual -Result PASS
}

function Get-PackageManifestIdentity {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [IO.Compression.ZipFile]::OpenRead($packagePath)
    try {
        $manifestEntry = $archive.Entries | Where-Object FullName -eq 'AppxManifest.xml' | Select-Object -First 1
        if ($null -eq $manifestEntry) {
            throw "The MSIX does not contain AppxManifest.xml: $packagePath"
        }

        $reader = [IO.StreamReader]::new($manifestEntry.Open())
        try {
            [xml] $manifestXml = $reader.ReadToEnd()
        } finally {
            $reader.Dispose()
        }
    } finally {
        $archive.Dispose()
    }

    $identity = $manifestXml.SelectSingleNode("/*[local-name()='Package']/*[local-name()='Identity']")
    if ($null -eq $identity) {
        throw "The MSIX manifest has no Package/Identity element: $packagePath"
    }

    [pscustomobject] @{
        Name = [string] $identity.Name
        Publisher = [string] $identity.Publisher
        Version = [string] $identity.Version
    }
}

function Get-InstalledPackage {
    param([Parameter(Mandatory)] [string] $PackageFamilyName)

    @(Get-AppxPackage -Name $expectedPackageName |
            Where-Object {
                $_.Publisher -eq $expectedPublisher -and
                $_.PackageFamilyName -eq $PackageFamilyName
            })
}

function Get-DpapiFiles {
    param([Parameter(Mandatory)] [string] $Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
        return @()
    }

    @(Get-ChildItem -LiteralPath $Path -File -Filter '*.bin' -Recurse -ErrorAction Stop)
}

try {
    Set-Content -LiteralPath $resultLog -Value "Pegasus MSIX install/uninstall test started $(Get-Date -Format o)" -Encoding utf8
    Write-Result -Command 'Package path' -Expected 'existing .msix file' -Actual $packagePath

    $identity = Get-PackageManifestIdentity
    Write-Result -Command 'MSIX Package/Identity' -Expected $expectedPackageName -Actual $identity.Name
    Write-Result -Command 'MSIX Identity/Publisher' -Expected $expectedPublisher -Actual $identity.Publisher
    Write-Result -Command 'MSIX Identity/Version' -Expected 'valid four-part version' -Actual $identity.Version
    Assert-Equal -Command 'MSIX package identity name' -Expected $expectedPackageName -Actual $identity.Name
    Assert-Equal -Command 'MSIX package publisher' -Expected $expectedPublisher -Actual $identity.Publisher
    $parsedVersion = $null
    Assert-True -Command 'MSIX package version format' -Condition ([Version]::TryParse($identity.Version, [ref] $parsedVersion)) -Actual $identity.Version

    $winapp = Get-Command winapp -ErrorAction Stop
    Write-Result -Command 'winapp --version' -Expected 'available' -Actual $winapp.Source

    $stalePackages = @(Get-AppxPackage -Name $expectedPackageName |
            Where-Object Publisher -eq $expectedPublisher)
    $stalePackageNames = @($stalePackages | Select-Object -ExpandProperty PackageFullName)
    $stalePackageActual = if ($stalePackageNames.Count -eq 0) { '<none>' } else { $stalePackageNames -join ', ' }
    Write-Result -Command "Get-AppxPackage -Name $expectedPackageName before install" -Expected '<none or stale package removed>' -Actual $stalePackageActual
    foreach ($stalePackage in $stalePackages) {
        Write-Result -Command "Remove-AppxPackage -Package $($stalePackage.PackageFullName)" -Expected 'removed' -Actual $stalePackage.PackageFullName
        Remove-AppxPackage -Package $stalePackage.PackageFullName -ErrorAction Stop
    }

    Write-Result -Command "Add-AppxPackage -Path $packagePath" -Expected 'installed for current user' -Actual $packagePath
    Add-AppxPackage -Path $packagePath -ErrorAction Stop

    $installedCandidates = @(Get-AppxPackage -Name $expectedPackageName |
            Where-Object {
                $_.Publisher -eq $expectedPublisher -and
                $_.Version.ToString() -eq $identity.Version
            })
    Assert-True -Command "Get-AppxPackage -Name $expectedPackageName after install" -Condition ($installedCandidates.Count -eq 1) -Actual $installedCandidates.Count
    $installed = $installedCandidates[0]
    $installedPackageFullName = [string] $installed.PackageFullName
    $installedPackageFamilyName = [string] $installed.PackageFamilyName
    Assert-Equal -Command 'installed package Name' -Expected $expectedPackageName -Actual $installed.Name
    Assert-Equal -Command 'installed package Publisher' -Expected $expectedPublisher -Actual $installed.Publisher
    Assert-Equal -Command 'installed package Version' -Expected $identity.Version -Actual $installed.Version
    Assert-True -Command 'installed package family name' -Condition (-not [string]::IsNullOrWhiteSpace($installedPackageFamilyName)) -Actual $installedPackageFamilyName
    Write-Result -Command 'installed package identity' -Expected "$expectedPackageName / $expectedPublisher / $identity.Version" -Actual "$($installed.Name) / $($installed.Publisher) / $($installed.PackageFamilyName) / $($installed.Version)"

    $packageFamilyPath = Join-Path $packageFamilyRootPath $installedPackageFamilyName
    Assert-True -Command "package family path after install: $packageFamilyPath" -Condition (Test-Path -LiteralPath $packageFamilyPath -PathType Container) -Actual (Test-Path -LiteralPath $packageFamilyPath -PathType Container)
    $storePath = if ([string]::IsNullOrWhiteSpace($DpapiStorePath)) {
        Join-Path $packageFamilyPath 'LocalState'
    } else {
        [IO.Path]::GetFullPath($DpapiStorePath)
    }
    Write-Result -Command 'DPAPI store path under test' -Expected 'package LocalState unless -DpapiStorePath is supplied' -Actual $storePath

    $launchFolder = if ([string]::IsNullOrWhiteSpace($LaunchInputFolder)) {
        [string] $installed.InstallLocation
    } else {
        [IO.Path]::GetFullPath($LaunchInputFolder)
    }
    Assert-True -Command 'launch input folder exists' -Condition (Test-Path -LiteralPath $launchFolder -PathType Container) -Actual $launchFolder
    $launchOutput = (& $winapp.Source run $launchFolder --detach --json 2>&1 | Out-String).Trim()
    $launchExitCode = $LASTEXITCODE
    Assert-Equal -Command "winapp run $launchFolder --detach --json exit code" -Expected 0 -Actual $launchExitCode
    Write-Result -Command "winapp run $launchFolder --detach --json output" -Expected 'process id' -Actual $launchOutput
    $pidMatch = [regex]::Match($launchOutput, '(?i)(?:"(?:pid|processId)"\s*:\s*|\bPID\s*[:=]\s*)(\d+)')
    Assert-True -Command 'winapp run returned a process id' -Condition $pidMatch.Success -Actual $launchOutput
    $launchProcessId = [int] $pidMatch.Groups[1].Value
    $launchedProcess = $null
    for ($attempt = 1; $attempt -le 10; $attempt++) {
        $launchedProcess = Get-Process -Id $launchProcessId -ErrorAction SilentlyContinue
        if ($null -ne $launchedProcess) {
            break
        }

        Start-Sleep -Milliseconds 200
    }
    Assert-True -Command 'launched process is running' -Condition ($null -ne $launchedProcess) -Actual $launchProcessId
    Write-Result -Command "Stop-Process -Id $launchProcessId" -Expected 'terminated after launch assertion' -Actual $launchedProcess.ProcessName
    Stop-Process -Id $launchProcessId -Force -ErrorAction SilentlyContinue
    $launchProcessId = $null

    Write-Result -Command "Remove-AppxPackage -Package $installedPackageFullName" -Expected 'uninstalled' -Actual $installedPackageFullName
    Remove-AppxPackage -Package $installedPackageFullName -ErrorAction Stop
    $remainingPackage = @()
    for ($attempt = 1; $attempt -le 30; $attempt++) {
        $remainingPackage = @(Get-InstalledPackage -PackageFamilyName $installedPackageFamilyName)
        if ($remainingPackage.Count -eq 0) {
            break
        }

        Start-Sleep -Milliseconds 500
    }
    $remainingPackageNames = @($remainingPackage | Select-Object -ExpandProperty PackageFullName)
    $remainingPackageActual = if ($remainingPackageNames.Count -eq 0) { '<none>' } else { $remainingPackageNames -join ', ' }
    Assert-True -Command "Get-AppxPackage -Name $expectedPackageName after uninstall" -Condition ($remainingPackage.Count -eq 0) -Actual $remainingPackageActual
    Assert-True -Command "package family cleanup: $packageFamilyPath" -Condition (-not (Test-Path -LiteralPath $packageFamilyPath)) -Actual (Test-Path -LiteralPath $packageFamilyPath)
    $remainingDpapiFiles = @(Get-DpapiFiles -Path $storePath)
    $remainingDpapiActual = if ($remainingDpapiFiles.Count -eq 0) { '<none>' } else { ($remainingDpapiFiles | ForEach-Object FullName) -join ', ' }
    Assert-True -Command "DPAPI store cleanup: $storePath" -Condition ($remainingDpapiFiles.Count -eq 0) -Actual $remainingDpapiActual

    Write-Result -Command 'install -> identity -> launch -> uninstall -> cleanup' -Expected 'complete' -Actual 'complete' -Result PASS
    Write-Host "Packaging test passed. Result log: $resultLog" -ForegroundColor Green
} catch {
    $scriptFailed = $true
    Write-Result -Command 'test execution' -Expected 'no terminating error' -Actual $_.Exception.Message -Result FAIL
    throw
} finally {
    if ($launchProcessId) {
        Stop-Process -Id $launchProcessId -Force -ErrorAction SilentlyContinue
    }

    if ($installedPackageFullName) {
        $cleanupPackage = Get-AppxPackage -PackageTypeFilter Main -Name $expectedPackageName |
            Where-Object {
                $_.PackageFullName -eq $installedPackageFullName -and
                $_.Publisher -eq $expectedPublisher
            }
        if ($cleanupPackage) {
            try {
                Remove-AppxPackage -Package $installedPackageFullName -ErrorAction Stop
                Write-Result -Command "failure cleanup Remove-AppxPackage -Package $installedPackageFullName" -Expected 'removed' -Actual $installedPackageFullName
            } catch {
                Write-Result -Command "failure cleanup Remove-AppxPackage -Package $installedPackageFullName" -Expected 'removed' -Actual $_.Exception.Message -Result FAIL
                if (-not $scriptFailed) {
                    throw
                }
            }
        }
    }
}
