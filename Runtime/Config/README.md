# Local Development Monitoring Stack

Simple monitoring for Unity development: Sentry (crash reporting) + Loki/Grafana (logs).

## Quick Start

```bash
cd Runtime/Config
docker compose up -d
```

## Setup Sentry Account

```bash
docker compose run --rm sentry-web createuser --email admin@localhost --password admin --superuser --no-input
```

Or use your own credentials:
```bash
docker compose run --rm sentry-web createuser --email your@email.com --password yourpassword --superuser --no-input
```

## Access Services

- **Sentry**: http://localhost:9000 (crash reports)
- **Grafana**: http://localhost:3000 (logs, no login required)

## Get Sentry DSN for Unity

1. Login to Sentry (http://localhost:9000)
2. Create a Unity/C# project
3. Copy the DSN (looks like: `http://abc123@localhost:9000/1`)
4. Add to `BLoggerConfig.asset`:
   - Enable Sentry: `true`
   - Sentry URL: `<paste DSN>`
   - Loki URL: `http://localhost:3100`

## Stop

```bash
docker compose down
```
