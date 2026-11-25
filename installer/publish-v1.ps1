# Đóng gói E-Tax V1: publish self-contained win-x64, kiểm tra file bí mật, nén ZIP, biên dịch installer nếu có Inno Setup.
# Chạy: powershell -NoProfile -ExecutionPolicy Bypass -File installer\publish-v1.ps1
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$root = Split-Path -Parent $PSScriptRoot
$csproj = Join-Path $root 'E-Tax\E-Tax.csproj'
$publishDir = Join-Path $root 'publish\E-Tax-win-x64'
$artifacts = Join-Path $root 'artifacts'

Write-Host '*** CANH BAO: PublicKeyPem trong ma nguon van la key DEV. Chu du an PHAI thay key that truoc khi phat hanh that (thay key lam mat hieu luc cac key da cap). ***' -ForegroundColor Yellow

# Phải kiểm tra trước dotnet publish vì csproj có target taskkill E-Tax.exe. Không tự kill.
if (Get-Process -Name 'E-Tax' -ErrorAction SilentlyContinue) {
    Write-Host 'E-Tax.exe dang chay. Dong ung dung roi chay lai; script khong tu dong tat.' -ForegroundColor Red
    exit 1
}

# Version lấy từ csproj, không hardcode bản thứ hai.
$m = [regex]::Match((Get-Content -LiteralPath $csproj -Raw), '<Version>\s*([^<\s]+)\s*</Version>')
if (-not $m.Success) { Write-Host 'Khong doc duoc <Version> trong E-Tax.csproj.' -ForegroundColor Red; exit 1 }
$version = $m.Groups[1].Value
Write-Host "Phien ban: $version"

if (Test-Path -LiteralPath $publishDir) { Remove-Item -LiteralPath $publishDir -Recurse -Force }

& dotnet publish $csproj -c Release -r win-x64 --self-contained true -o $publishDir
if ($LASTEXITCODE -ne 0) { Write-Host 'dotnet publish that bai.' -ForegroundColor Red; exit 1 }

# Kiểm tra file bí mật chỉ theo tên file; không đọc nội dung, không xóa.
$secrets = @(Get-ChildItem -LiteralPath $publishDir -Recurse -File -Force | Where-Object {
    $_.Name -ieq 'appsettings.json' -or $_.Name -like '*.pem' -or $_.Name -like '*.pfx' -or ($_.Name -like '*.private.*' -and $_.Extension -notin '.dll', '.pdb')  # bo qua System.Private.*.dll cua .NET
})
if ($secrets.Count -gt 0) {
    Write-Host 'DUNG: output co file nghi la bi mat (khong xoa, khong dong goi):' -ForegroundColor Red
    $secrets | ForEach-Object { Write-Host "  $($_.FullName)" -ForegroundColor Red }
    exit 1
}

New-Item -ItemType Directory -Force -Path $artifacts | Out-Null
$zip = Join-Path $artifacts "E-Tax-$version-win-x64.zip"
if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip -Force }
Compress-Archive -Path (Join-Path $publishDir '*') -DestinationPath $zip
Write-Host "Da tao ZIP: $zip"

# Installer (tùy chọn): chỉ biên dịch nếu có ISCC.
$iscc = $null
$cmd = Get-Command ISCC.exe -ErrorAction SilentlyContinue
if ($cmd) { $iscc = $cmd.Source }
else {
    foreach ($p in @("${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe", "$env:ProgramFiles\Inno Setup 6\ISCC.exe", "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe")) {
        if ($p -and (Test-Path -LiteralPath $p)) { $iscc = $p; break }
    }
}
if ($iscc) {
    & $iscc "/DAppVersion=$version" "/DSourceDir=$publishDir" "/DOutputDir=$artifacts" (Join-Path $PSScriptRoot 'E-Tax.iss')
    if ($LASTEXITCODE -ne 0) { Write-Host 'ISCC bien dich that bai.' -ForegroundColor Red; exit 1 }
    Write-Host "Da tao installer trong $artifacts"
} else {
    Write-Host 'Khong tim thay ISCC (Inno Setup): installer CHUA duoc bien dich. Chi co file ZIP.' -ForegroundColor Yellow
}

Write-Host '*** Nhac lai: thay key DEV (PublicKeyPem) truoc khi phat hanh that. Chua ky so (code signing) goi nao. ***' -ForegroundColor Yellow
exit 0
