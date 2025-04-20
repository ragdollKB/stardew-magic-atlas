param (
    [switch]$NoDeploy,
    [switch]$Deploy
)

# Stop Stardew Valley if it's running
$stardewProcess = Get-Process -Name "Stardew Valley" -ErrorAction SilentlyContinue
if ($stardewProcess) {
    Write-Host "Closing Stardew Valley..." -ForegroundColor Yellow
    $stardewProcess | Stop-Process -Force
    Start-Sleep -Seconds 2  # Give it time to fully close
}

# Set deployment flags based on parameters
if ($NoDeploy) {
    Write-Host "Building without deployment..." -ForegroundColor Cyan
    dotnet build .\WarpMod.csproj /p:DisableModDeploy=true
}
elseif ($Deploy) {
    # Force deployment even if files are in use (by deleting them first)
    $modDir = "E:\SteamLibrary\steamapps\common\Stardew Valley\Mods\Magic Atlas"
    if (Test-Path $modDir) {
        Write-Host "Cleaning mod directory..." -ForegroundColor Cyan
        Remove-Item -Path "$modDir\*" -Force -Recurse -ErrorAction SilentlyContinue
    }
    Write-Host "Building with forced deployment..." -ForegroundColor Cyan
    dotnet build .\WarpMod.csproj
}
else {
    # Default build
    Write-Host "Building with normal settings..." -ForegroundColor Cyan
    dotnet build .\WarpMod.csproj
}

# === Content Patcher Content Pack Deployment ===
$cpPackName = "[CP] Magic Atlas"
$cpSource = Join-Path $PSScriptRoot $cpPackName
$cpDest = "E:\SteamLibrary\steamapps\common\Stardew Valley\Mods\$cpPackName"

# Diagnostic output to help debug path issues
Write-Host "[DEBUG] cpSource: $cpSource" -ForegroundColor Magenta
Write-Host "[DEBUG] cpDest: $cpDest" -ForegroundColor Magenta
if (Test-Path -LiteralPath $cpSource) {
    Write-Host "[DEBUG] cpSource exists. Listing contents:" -ForegroundColor Magenta
    Get-ChildItem -LiteralPath $cpSource | ForEach-Object { Write-Host $_.FullName -ForegroundColor Magenta }
}
else {
    Write-Host "[DEBUG] cpSource does NOT exist!" -ForegroundColor Red
}

if (Test-Path -LiteralPath $cpSource) {
    Write-Host "Copying Content Patcher pack: $cpPackName..." -ForegroundColor Cyan
    if (Test-Path -LiteralPath $cpDest) {
        Remove-Item -LiteralPath $cpDest -Force -Recurse -ErrorAction SilentlyContinue
    } else {
        New-Item -ItemType Directory -Path $cpDest | Out-Null
    }
    Get-ChildItem -LiteralPath $cpSource | Copy-Item -Destination $cpDest -Recurse -Force
    Write-Host "Content Patcher pack deployed to Mods folder." -ForegroundColor Green
} else {
    Write-Warning "Content Patcher pack folder '$cpPackName' not found in the project root. Custom items will NOT work unless this pack is present in your Mods folder!"
}

Write-Host "Build process completed." -ForegroundColor Green
