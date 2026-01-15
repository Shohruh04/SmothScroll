# SmoothScroll

A lightweight Windows utility that adds smooth, momentum-based scrolling to your mouse wheel system-wide.

## Features

- **Smooth scrolling** - Transforms jerky mouse wheel steps into fluid, momentum-based scrolling
- **System tray app** - Runs quietly in the background with easy access
- **Adjustable settings** - Configure scroll speed and smoothness to your preference
- **Start with Windows** - Optional auto-start on login
- **Enable/Disable toggle** - Quickly turn smooth scrolling on or off

## Requirements

- Windows 10/11
- .NET 8.0 Runtime

## Installation

Download the latest release from the [Releases](../../releases) page, or build from source:

```bash
dotnet build -c Release
```

## Usage

1. Run `SmoothScroll.exe`
2. The app appears in your system tray
3. Right-click the tray icon for options:
   - **Enable/Disable** - Toggle smooth scrolling
   - **Settings** - Adjust speed and smoothness
   - **Start with Windows** - Enable auto-start
   - **Exit** - Close the application

## Settings

| Setting | Description |
|---------|-------------|
| **Speed** | How fast the scroll responds to wheel input |
| **Smoothness** | How long the scroll momentum lasts (higher = longer glide) |

## Building

```bash
# Debug build
dotnet build

# Release build
dotnet build -c Release

# Self-contained executable
dotnet publish -c Release -r win-x64 --self-contained
```

## License

MIT
