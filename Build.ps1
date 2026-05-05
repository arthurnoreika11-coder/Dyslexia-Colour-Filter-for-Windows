# Build script for Dyslexia Color Filter
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptDir

$outputDir = "$scriptDir\WinForms\bin"
$outputFile = "$outputDir\DyslexiaFilter.exe"
$sourceFile = "$scriptDir\WinForms\winForms.cs"

if (!(Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

$csc = "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"

Write-Host "Compiling..."

& $csc /target:winexe `
       /out:$outputFile `
       /r:System.dll `
       /r:System.Windows.Forms.dll `
       /r:System.Drawing.dll `
       $sourceFile

if ($LASTEXITCODE -eq 0) {
    Write-Host "SUCCESS: $outputFile" -ForegroundColor Green
} else {
    Write-Host "FAILED" -ForegroundColor Red
}

Read-Host "Press Enter to close"
