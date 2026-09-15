# deploy.ps1 — Fast deploy: zip code only → copy 1 file → extract on VPS
param(
    [string]$VpsIp   = "103.153.69.217",
    [string]$VpsUser = "Administrator",
    [string]$VpsPass = "smo@9728",
    [string]$AppPool = "Mitech",
    [string]$AppPath = "C:\apps\mitech"
)

$ErrorActionPreference = "Stop"
Set-Location (Split-Path $MyInvocation.MyCommand.Path)

# ── 1. Build ──────────────────────────────────────────────────────────────────
Write-Host "[1/4] Building..." -ForegroundColor Cyan
dotnet publish src/Mitech.Web/Mitech.Web.csproj -c Release -o publish_out --nologo -v quiet
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

# ── 2. Zip (code only — skip large static assets that rarely change) ──────────
Write-Host "[2/4] Zipping code files..." -ForegroundColor Cyan
$zip    = ".\deploy.zip"
$srcDir = ".\publish_out"
Remove-Item $zip -Force -ErrorAction SilentlyContinue

# Collect files: everything EXCEPT uploads/ and large media in wwwroot/images/
$skipExts  = @(".mp4", ".mov", ".avi", ".webm")
$skipDirs  = @("uploads")

$files = Get-ChildItem -Path $srcDir -Recurse -File | Where-Object {
    $rel = $_.FullName.Substring((Resolve-Path $srcDir).Path.Length + 1)
    $inSkipDir  = $skipDirs  | Where-Object { $rel -like "$_\*" }
    $isSkipExt  = $skipExts  -contains $_.Extension.ToLower()
    $isProdJson = $_.Name -eq "appsettings.Production.json"
    -not $inSkipDir -and -not $isSkipExt -and -not $isProdJson
}

# Create zip from filtered file list using temp staging folder
$stage = ".\deploy_stage"
Remove-Item $stage -Recurse -Force -ErrorAction SilentlyContinue
foreach ($f in $files) {
    $rel  = $f.FullName.Substring((Resolve-Path $srcDir).Path.Length + 1)
    $dest = Join-Path $stage $rel
    $destDir = Split-Path $dest -Parent
    if (-not (Test-Path $destDir)) { New-Item -ItemType Directory -Path $destDir -Force | Out-Null }
    Copy-Item $f.FullName $dest -Force
}
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($stage, (Resolve-Path ".").Path + "\deploy.zip")
Remove-Item $stage -Recurse -Force

$sizeMB = [math]::Round((Get-Item $zip).Length / 1MB, 1)
Write-Host "   → deploy.zip: $sizeMB MB ($($files.Count) files)"

# ── 3. Connect & deploy ───────────────────────────────────────────────────────
Write-Host "[3/4] Uploading & deploying to VPS..." -ForegroundColor Cyan
$pass = ConvertTo-SecureString $VpsPass -AsPlainText -Force
$cred = New-Object PSCredential($VpsUser, $pass)
$opt  = New-PSSessionOption -SkipCACheck -SkipCNCheck
$s    = New-PSSession -ComputerName $VpsIp -Credential $cred -SessionOption $opt -Port 5985

# Backup production appsettings & stop app pool
Invoke-Command -Session $s -ScriptBlock {
    param($ap)
    $prod = "$ap\appsettings.Production.json"
    if (Test-Path $prod) { Copy-Item $prod "C:\Windows\Temp\appsettings.Production.json.bak" -Force }
    & "C:\Windows\System32\inetsrv\appcmd.exe" stop apppool /apppool.name:"Mitech" 2>&1 | Out-Null
    Start-Sleep -Seconds 3
} -ArgumentList $AppPath

# Copy single zip file to VPS (fixed remote path)
Copy-Item -Path $zip -Destination "C:\Windows\Temp\deploy.zip" -ToSession $s -Force
Write-Host "   → Upload complete"

# Extract & restore production settings & start
Invoke-Command -Session $s -ScriptBlock {
    param($ap)
    Expand-Archive -Path "C:\Windows\Temp\deploy.zip" -DestinationPath $ap -Force
    $bak = "C:\Windows\Temp\appsettings.Production.json.bak"
    if (Test-Path $bak) {
        Copy-Item $bak "$ap\appsettings.Production.json" -Force
        Remove-Item $bak -Force
    }
    Remove-Item "C:\Windows\Temp\deploy.zip" -Force
    & "C:\Windows\System32\inetsrv\appcmd.exe" start apppool /apppool.name:"Mitech"
} -ArgumentList $AppPath

Remove-PSSession $s

# ── 4. Cleanup ────────────────────────────────────────────────────────────────
Remove-Item $zip -Force -ErrorAction SilentlyContinue
Write-Host "[4/4] Done!" -ForegroundColor Green
