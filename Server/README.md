# Simple Self-Hosted Monitoring Stack

Lightweight log aggregation with Loki + Grafana + Promtail. No Sentry, no Kafka, no Python - just Docker!

## Quick Start

```bash
cd Server
docker compose up -d
```

## Access Services

- **Grafana**: http://localhost:3000 (log visualization, no login required)
- **Loki API**: http://localhost:3100 (log storage)

## Unity Configuration

In `Runtime/Resources/BLoggerConfig.asset`:
- `Loki Url`: `http://localhost:3100`

LokiHandler automatically sends all Unity logs to Loki. View them in Grafana!

## Grafana Setup

1. Go to http://localhost:3000
2. Click "Explore" (compass icon)
3. Select "Loki" data source
4. Query logs: `{container="unity-client"}`

## Stop

```bash
docker compose down
```
