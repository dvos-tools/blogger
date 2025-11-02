# BLogger Configuration

This directory contains the default configuration asset for BLogger.

## Configuration Asset

- **`BLoggerConfig.asset`**: Main configuration for BLogger handlers (File, OnScreen, Loki)

This asset is loaded from `Resources/` at runtime and controls all logging behavior.

## Setting Up Monitoring (Optional)

BLogger can integrate with external monitoring services:

- **Loki**: For log aggregation and visualization

### For Local Development/Testing

If you want to run a local monitoring server for testing BLogger, see:

👉 **[/Server/README.md](/Server/README.md)** - Instructions for setting up a local Loki + Grafana stack

### For Production Use

Configure `BLoggerConfig.asset` to point to your production monitoring infrastructure:

1. **Loki** (Log Aggregation):
   - Set up Loki (self-hosted or Grafana Cloud)
   - Set `Loki Url` to your Loki endpoint (e.g., `https://your-loki.com:3100`)

## Default Configuration

Out of the box, BLogger provides:
- ✅ **File Handler**: Logs to `Application.persistentDataPath/logs/`
- ✅ **OnScreen Terminal**: Press `` ` `` (backtick) to toggle in-game console
- ❌ **Loki**: No default URL (configure endpoint to enable)
- ❌ **Sentry**: Optional handler (won't crash if Sentry SDK not installed)

All handlers can be enabled/disabled and configured via the `BLoggerConfig.asset`.
