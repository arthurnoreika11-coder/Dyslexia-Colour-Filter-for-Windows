# Build script for Dyslexia Color Filter
$outputDir = "WinForms\bin"
$outputFile = "$outputDir\DyslexiaFilter.exe"

if (!(Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

Write-Host "Compiling Dyslexia Color Filter..."
csc /target:winexe /out:$outputFile WinForms/winForms.cs

if ($?) {
    Write-Host "✓ Build successful: $outputFile" -ForegroundColor Green
} else {
    Write-Host "✗ Build failed" -ForegroundColor Red
}
