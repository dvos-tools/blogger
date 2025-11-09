# BLogger - Better Unity Logger

A powerful logging system for Unity with in-game terminal, file logging, and runtime value inspection.

## Installation

Add to your Packages/manifest.json:
```
{
  "dependencies": {
    "com.dvos-tools.blogger": "https://github.com/dvos-tools/blogger.git"
  }
}
```

## Configuration

On first import, BLogger automatically creates `Assets/Resources/BLoggerConfig.asset` with default settings.

Edit this config to control:
- **Handlers**: Enable/disable File, Terminal, or Loki logging
- **Terminal Settings**: Toggle key, max log entries, scroll behavior
- **File Logging**: Set log path, file naming, and rotation
- **Input System**: Choose Legacy or New Input System
- **Sampling**: Control log volume in production builds

Quick access: `Tools > BLogger > Open Config`

## Terminal Usage

Press `~` (tilde) to open the in-game terminal. Use built-in commands:
- `/clear` - Clear terminal output
- `/copy` - Copy logs to clipboard
- `/context` - Show current logging context
- `/help` - Show available commands
- `/exit` or `/quit` - Close terminal
- `cmd/control [+ / -]` - Increase or decrease font size

Press `Tab` while typing a command to see available options and auto-complete suggestions.

## BLogger Attributes

Mark classes and methods to expose them in the terminal:

```csharp
[BLoggerAggregate("Players")]  // Group players under "Players" namespace
public class PlayerStats : BLoggerMonoBehaviour
{
    [BLoggerAggregateId] public string playerId = "player1";  // Unique identifier
    [BLoggerValue] public int health = 100;                   // Readable value
    [BLoggerValue] public int maxHealth = 100;                // Readable value
    
    [BLoggerAction]  // Executable action
    public void Heal(int amount) 
    { 
        health = Mathf.Min(health + amount, maxHealth);
        BLogger.Log($"{playerId} healed for {amount}. Health: {health}");
    }
}

// Terminal Usage:
// "/Players.player1.health"      -> Shows current health value
// "/Players.player1.Heal(50)"    -> Heals player1 by 50 HP
```

## Basic Logging

```csharp
BLogger.Log("Info message");
BLogger.Warn("Warning message");
BLogger.Error("Error message");
```

> [!NOTE]
> This package is under active development. Breaking changes may occur before v2.0.
