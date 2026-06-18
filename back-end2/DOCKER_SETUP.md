# Trap-Intel Docker Setup

This document describes how to run Trap-Intel using Docker Compose with PostgreSQL, Seq, Mailpit, and OpenTelemetry Collector.

## Services Overview

| Service | Port | Description |
|---------|------|-------------|
| **trap-intel-api** | 5000 (HTTP), 5001 (HTTPS) | Main API application |
| **postgres** | 5432 | PostgreSQL 16 database |
| **seq** | 5341 | Seq logging UI |
| **mailpit** | 8025 (UI), 1025 (SMTP) | Email testing server |
| **otel-collector** | 4317 (OTLP gRPC), 4318 (OTLP HTTP), 8889 (Prometheus) | Metrics collector/export bridge |

## Quick Start

### 1. Start All Services
```bash
docker-compose up -d
```

### 2. View Logs
```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f trap-intel-api
```

### 3. Stop Services
```bash
docker-compose down

# Stop and remove volumes (clean slate)
docker-compose down -v
```

## Accessing Services

### API
- **HTTP**: http://localhost:5000
- **HTTPS**: https://localhost:5001
- **Health Check**: http://localhost:5000/health
- **OpenAPI/Swagger**: http://localhost:5000/openapi/v1.json
- **Prometheus Metrics**: http://localhost:5000/metrics

### OpenTelemetry Collector
- **OTLP gRPC Receiver**: localhost:4317
- **OTLP HTTP Receiver**: localhost:4318
- **Prometheus Exporter Metrics**: http://localhost:8889/metrics

### Seq (Logging)
- **Web UI**: http://localhost:5341
- No password required for development

### Mailpit (Email Testing)
- **Web UI**: http://localhost:8025
- **SMTP**: localhost:1025

### PostgreSQL
- **Host**: localhost
- **Port**: 5432
- **Database**: trapintel
- **Username**: trapintel_user
- **Password**: TrapIntel_Secure_2024!

Connection string for tools:
```
Host=localhost;Port=5432;Database=trapintel;Username=trapintel_user;Password=TrapIntel_Secure_2024!
```

## Database Migrations

The API automatically applies EF Core migrations on startup in Docker/Development mode.

## Database Seeding

Seed data is applied by the API startup pipeline (not by PostgreSQL init SQL scripts):

1. EF Core migrations are applied.
2. `DatabaseSeederOrchestrator` executes all registered seeders in order.

Notes:

- PostgreSQL init script `01-init-database.sql` only creates schema/extensions/privileges.
- `02-seed-data.sql` is intentionally a no-op for backward compatibility.
- To reseed from scratch, remove volumes and start again.

### Manual Migration Commands
```bash
# Restore local EF tool (once per machine / after clone)
dotnet tool restore

# Optional if your machine has only newer .NET runtime major installed
# PowerShell: $env:DOTNET_ROLL_FORWARD="Major"

# Create a new migration
dotnet tool run dotnet-ef migrations add <MigrationName> --project Trap-Intel.Infrastructure --startup-project Trap-Intel.Api

# Apply migrations
dotnet tool run dotnet-ef database update --project Trap-Intel.Infrastructure --startup-project Trap-Intel.Api

# Generate SQL script
dotnet tool run dotnet-ef migrations script --project Trap-Intel.Infrastructure --startup-project Trap-Intel.Api
```

## Environment Variables

The following environment variables can be configured:

| Variable | Default | Description |
|----------|---------|-------------|
| `ConnectionStrings__DefaultConnection` | (see docker-compose.yml) | PostgreSQL connection string |
| `Serilog__SeqServerUrl` | http://seq:5341 | Seq server URL |
| `EmailSettings__SmtpHost` | mailpit | SMTP host for email |
| `EmailSettings__SmtpPort` | 1025 | SMTP port |
| `EmailSettings__SenderEmail` | noreply@trapintel.local | Sender email |
| `OpenTelemetry__Otlp__Endpoint` | http://otel-collector:4317 | OTLP collector endpoint |
| `OpenTelemetry__Otlp__Protocol` | Grpc | OTLP transport protocol |
| `Billing__OverdueProcessing__RunOnStartup` | true (Docker appsettings) | Run overdue processing once at startup to validate billing metrics without waiting for daily schedule |

## Development Workflow

### Rebuild API After Code Changes
```bash
docker-compose up -d --build trap-intel-api
```

### Reset Database
```bash
# Stop services and remove volumes
docker-compose down -v

# Start fresh
docker-compose up -d
```

### Connect to PostgreSQL Container
```bash
docker exec -it trap-intel-postgres psql -U trapintel_user -d trapintel
```

### View Schema
```sql
-- List all tables in trapintel schema
SELECT table_name FROM information_schema.tables 
WHERE table_schema = 'trapintel';

-- Describe a table
\d trapintel.organizations
```

## Troubleshooting

### API Won't Start
1. Check PostgreSQL is healthy: `docker-compose ps`
2. View API logs: `docker-compose logs trap-intel-api`
3. Verify database connection: `docker exec -it trap-intel-postgres pg_isready`

### Database Migration Errors
1. Check migration logs in Seq: http://localhost:5341
2. Connect to database and check schema exists
3. Try dropping database and starting fresh: `docker-compose down -v && docker-compose up -d`

### Port Conflicts
If ports are in use, modify `docker-compose.yml` or `docker-compose.override.yml`:
```yaml
services:
  postgres:
    ports:
      - "5433:5432"  # Change external port
```

## Production Considerations

For production deployment:

1. **Secrets**: Use Docker secrets or environment-specific configuration
2. **Volumes**: Map volumes to persistent storage
3. **Networking**: Use proper network isolation
4. **Health Checks**: Configure appropriate intervals
5. **Logging**: Configure Seq retention policies
6. **SSL**: Add SSL certificates for HTTPS
