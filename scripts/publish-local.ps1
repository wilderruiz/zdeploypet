[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$releaseRoot = Join-Path (Split-Path -Parent $repoRoot) 'ZDeployPet_Releases'
$currentRoot = Join-Path $releaseRoot 'current'
$stagingRoot = Join-Path $repoRoot 'artifacts\local-publish'
$publishedExe = Join-Path $stagingRoot 'ZDeployPet.exe'
$currentExe = Join-Path $currentRoot 'ZDeployPet.exe'

$running = Get-Process -Name 'ZDeployPet' -ErrorAction SilentlyContinue
if ($running) {
    $details = $running | ForEach-Object { "PID $($_.Id): $($_.Path)" }
    throw "ZDeployPet is still running. Close it before publishing.`n$($details -join [Environment]::NewLine)"
}

Push-Location $repoRoot
try {
    dotnet build ZDeployPet.sln -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Release build failed.' }

    dotnet run --project tests\ZDeployPet.Core.Tests\ZDeployPet.Core.Tests.csproj -c Release --no-build
    if ($LASTEXITCODE -ne 0) { throw 'Core tests failed.' }

    dotnet run --project tests\ZDeployPet.Infrastructure.Tests\ZDeployPet.Infrastructure.Tests.csproj -c Release --no-build
    if ($LASTEXITCODE -ne 0) { throw 'Infrastructure tests failed.' }

    if (Test-Path -LiteralPath $stagingRoot) {
        Remove-Item -LiteralPath $stagingRoot -Recurse -Force
    }

    dotnet publish src\ZDeployPet.App\ZDeployPet.App.csproj `
        -c Release `
        -r win-x64 `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:DebugType=None `
        -o $stagingRoot
    if ($LASTEXITCODE -ne 0) { throw 'Local publish failed.' }
    if (-not (Test-Path -LiteralPath $publishedExe -PathType Leaf)) {
        throw "Published executable was not produced: $publishedExe"
    }

    New-Item -ItemType Directory -Path $currentRoot -Force | Out-Null
    Copy-Item -LiteralPath $publishedExe -Destination $currentExe -Force
    $hash = (Get-FileHash -LiteralPath $currentExe -Algorithm SHA256).Hash

    Write-Host ''
    Write-Host 'ZDeployPet local development build is ready:' -ForegroundColor Green
    Write-Host $currentExe
    Write-Host "SHA-256: $hash"
}
finally {
    Pop-Location
}
