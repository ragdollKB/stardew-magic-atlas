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
    $modDir = "E:\SteamLibrary\steamapps\common\Stardew Valley\Mods\WarpMod"
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

Write-Host "Build process completed." -ForegroundColor Green
