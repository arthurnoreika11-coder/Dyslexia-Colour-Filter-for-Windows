# Dyslexia Colour Filter for Windows

A personal Windows accessibility and legal technology learning project for a subtle coloured screen overlay to support dyslexia, visual stress, and more comfortable on-screen reading.

## The PowerShell C# Approach

macOS includes colour filters in Accessibility settings. Windows has accessibility tooling too, but not custom colour, but this project is a hands-on way to learn Windows desktop development while exploring a practical adjustment that could matter in long reading-heavy work.

I am building this from lived experience as a dyslexic person who can struggle with extended on-screen reading, and as an aspiring legal technologist / legal engineer relearning coding.

This is not intended to bypass workplace IT controls. If used on a work laptop, this repository should be shared with IT, security, or workplace adjustments teams for review first.

## Building & Compilation

### Quick Build with PowerShell

Run the included build script:

```powershell
.\Build.ps1
```

This will:
- Create the `WinForms\bin` output directory
- Compile the C# code to `WinForms\bin\DyslexiaFilter.exe`
- Show a success or error message

**Note:** If you encounter an execution policy error, run this first:
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope Process
```

### Manual Compilation

If you prefer to compile manually, use the C# compiler:

```bash
csc /target:winexe /out:WinForms\bin\DyslexiaFilter.exe WinForms\winForms.cs
```

Or with .NET SDK:

```bash
dotnet build -c Release
```

## Next Goal:

