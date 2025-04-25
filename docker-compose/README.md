# Navigation Platform - Docker Compose Setup

This directory contains Docker Compose configuration for running the Navigation Platform application with its required infrastructure and observability stack.

## Components

The Docker Compose setup includes:

1. **API**: The main Navigation Platform API
2. **Database**: SQL Server database
3. **Jaeger**: Distributed tracing visualization
4. **Prometheus**: Metrics collection and alerting

## Getting Started

### Prerequisites

- Docker
- Docker Compose

### Running the Stack

To start all services:

```bash
docker-compose up -d
```

To view logs:

```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f api
```

To stop all services:

```bash
docker-compose down
```

To stop and remove volumes:

```bash
docker-compose down -v
```

## Accessing Components

### Navigation Platform API

- **URL**: http://localhost:5000
- **Swagger UI**: http://localhost:5000/swagger
- **Health Checks**:
  - Liveness: http://localhost:5000/healthz
  - Readiness: http://localhost:5000/readyz
- **Metrics**: http://localhost:5000/metrics

### Jaeger UI

- **URL**: http://localhost:16686
- Use the UI to:
  - Search for traces by service, operation, or tags
  - View trace timelines and spans
  - Analyze service dependencies

### Prometheus

- **URL**: http://localhost:9090
- Use the UI to:
  - Query metrics using PromQL
  - View alert rules
  - Check target statuses

## Observability Features

### Structured Logging

The API produces structured JSON logs that include:
- Timestamp
- Log level
- Request details
- Correlation ID
- Exception details (if applicable)

### Distributed Tracing

Traces from the API are sent to Jaeger, allowing:
- End-to-end visibility of requests
- Performance bottleneck identification
- Correlation of logs with traces via correlation ID

### Metrics

Prometheus collects metrics from the API, including:
- HTTP request latency
- Database query latency
- Error rates

### Alerts

Prometheus is configured with alert rules:
- Queue lag alert triggers when message queue lag exceeds 100 messages for 5+ minutes
- Alert rules are defined in `prometheus/alert.rules.yml`

## Customization

To customize the Docker Compose setup:

1. Environment variables can be modified in `docker-compose.yml`
2. Prometheus configuration can be modified in `prometheus/prometheus.yml`
3. Alert rules can be modified in `prometheus/alert.rules.yml`


