# Monitoring Stack

Loki + Grafana + Promtail for Unity log aggregation.

## Quick Start

```bash
cd Server
cp .env.example .env  # Edit to change password!
docker compose up -d
```

## Access

- **Grafana**: http://localhost:3000 (Login: `admin`/`admin`)
- **Loki**: http://localhost:3100

## Unity Config

Set `Loki Url` to `http://localhost:3100` in BLoggerConfig.

## Stop

```bash
docker compose down
```
