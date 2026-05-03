# Dyslexia Colour Filter for Windows

A personal Windows accessibility and legal technology learning project for a subtle coloured screen overlay to support dyslexia, visual stress, and more comfortable on-screen reading.

## Why Build This?

macOS includes colour filters in Accessibility settings. Windows has accessibility tooling too, but this project is a hands-on way to learn Windows desktop development while exploring a practical adjustment that could matter in long reading-heavy work.

I am building this from lived experience as a dyslexic person who can struggle with extended on-screen reading, and as an aspiring legal technologist / legal engineer relearning coding.

This is not intended to bypass workplace IT controls. If used on a work laptop, this repository should be shared with IT, security, or workplace adjustments teams for review first.

## Current Status

The repository currently contains two working learning prototypes:

- `DyslexiaFilter.ps1`
- `WinForms/winForms.cs`

They demonstrate several important behaviours:

- transparent colour overlay
- click-through overlay
- tray icon
- settings window
- colour picker
- opacity slider
- enable/disable toggle
- local settings file

The long-term learning plan still aims toward a modern WPF version with cleaner structure, styling, multi-monitor support, polish, and documentation.

## Build the Current Prototype

### PowerShell Prototype

On Windows, from the repository root:

```powershell
powershell.exe -NoProfile -STA -ExecutionPolicy Bypass -File .\DyslexiaFilter.ps1
```

### C# WinForms Prototype

On Windows, from the repository root:

```bat
"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /target:winexe /out:ColourFilter.exe "WinForms\winForms.cs" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll
```

Then run:

```bat
ColourFilter.exe
```

Use the tray icon near the Windows clock to open settings or exit the app.

## Settings File

The prototype saves settings next to the executable:

```text
filter-settings.txt
```

Example:

```text
Colour=#FFF2A8
Opacity=0.5
Enabled=True
```

## Learning Roadmap

The full learning plan is in:

- [LEARNING-MILESTONES.md](LEARNING-MILESTONES.md)

Supporting notes:

- [TEST-NOTES.md](TEST-NOTES.md)
- [STAGE-1-FILTERSETTINGS.md](STAGE-1-FILTERSETTINGS.md)
- [IT-Review.md](IT-Review.md)

## Project Direction

This project has two tracks:

- A working prototype track, so every stage produces something that can be tested.
- A learning track, so each stage records what was built, what was confusing, what was fixed, and what was learned.

That means some early versions may be intentionally simple. The point is to grow the project safely, clearly, and in a way that can be explained to non-developers and workplace IT reviewers.
