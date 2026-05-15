$ErrorActionPreference = "Stop"

$src = $PSScriptRoot
$dst = "F:\SteamLibrary\steamapps\common\Barotrauma\LocalMods\Cyclops Voiceovers"

Write-Host "Building client..." -ForegroundColor Cyan
dotnet build "$src\CSharp\Client\WindowsClient.csproj" -c Release -p:Platform=x64 -v:q --nologo
if ($LASTEXITCODE -ne 0) { Write-Host "Client build FAILED." -ForegroundColor Red; exit 1 }

Write-Host "Building server..." -ForegroundColor Cyan
dotnet build "$src\CSharp\Server\WindowsServer.csproj" -c Release -p:Platform=x64 -v:q --nologo
if ($LASTEXITCODE -ne 0) { Write-Host "Server build FAILED." -ForegroundColor Red; exit 1 }

Write-Host "Syncing to LocalMods..." -ForegroundColor Cyan
Get-ChildItem -Path $src -Recurse -File | Where-Object {
    $_.FullName -notmatch "\\CSharp\\" -and $_.FullName -notmatch "\\Ref\\"
} | ForEach-Object {
    $rel    = $_.FullName.Substring($src.Length + 1)
    $target = Join-Path $dst $rel
    New-Item -ItemType Directory -Path (Split-Path $target -Parent) -Force | Out-Null
    Copy-Item -Path $_.FullName -Destination $target -Force
}

Write-Host "Done." -ForegroundColor Green
