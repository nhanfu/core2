# Docker Compose Setup Guide

This project includes a complete Docker Compose setup with:
- **Frontend (app)**: React/Vite application on Nginx
- **API (CoreAPI)**: .NET 9 API application
- **Database (db)**: PostgreSQL 16 Alpine

## Prerequisites

- Docker Desktop installed
- Docker Compose v2.x or higher

## Quick Start

### 1. Build and Start All Services

```bash
docker-compose -f docker-compose-new.yml up -d --build
```

### 2. View Logs

```bash
# All services
docker-compose -f docker-compose-new.yml logs -f

# Specific service
docker-compose -f docker-compose-new.yml logs -f api
docker-compose -f docker-compose-new.yml logs -f app
docker-compose -f docker-compose-new.yml logs -f db
```

### 3. Stop All Services

```bash
docker-compose -f docker-compose-new.yml down
```

### 4. Stop and Remove Volumes (Clean Start)

```bash
docker-compose -f docker-compose-new.yml down -v
```

## Service URLs

- **Frontend**: http://localhost (port 80)
- **API**: http://localhost:8080
- **PostgreSQL**: localhost:5432
  - Database: `crm`
  - Username: `postgres`
  - Password: `YourStrongPassword123!`

## Database Connection

### From Host Machine

```bash
psql -h localhost -p 5432 -U postgres -d crm
```

### Connection String (from host)

```
Host=localhost;Port=5432;Database=crm;Username=postgres;Password=YourStrongPassword123!;
```

### Connection String (from API container)

```
Host=db;Port=5432;Database=crm;Username=postgres;Password=YourStrongPassword123!;
```

## Development Workflow

### Rebuild Specific Service

```bash
# Rebuild API only
docker-compose -f docker-compose-new.yml up -d --build api

# Rebuild Frontend only
docker-compose -f docker-compose-new.yml up -d --build app
```

### Execute Commands in Container

```bash
# Access API container
docker exec -it core2-api bash

# Access PostgreSQL
docker exec -it core2-postgres psql -U postgres -d crm

# Access Frontend container
docker exec -it core2-frontend sh
```

### Database Migration

If you need to run database migrations:

```bash
# From host (if you have .NET SDK installed)
cd CoreAPI
dotnet ef database update

# Or from API container
docker exec -it core2-api dotnet ef database update
```

## Project Structure

```
.
├── CoreAPI/
│   ├── dockerfile-new          # API Dockerfile
│   ├── CoreAPI.csproj          # Updated with Npgsql packages
│   ├── Program.cs              # Configured for PostgreSQL
│   └── appsettings.json        # PostgreSQL connection string
├── frontend/
│   ├── dockerfile-new          # Frontend Dockerfile
│   ├── nginx.conf              # Nginx configuration
│   └── ...
└── docker-compose-new.yml      # Main compose file
```

## Volumes

The following volumes are created for data persistence:

- `postgres_data`: PostgreSQL database files
- `api_uploads`: API upload files
- `api_excel`: API Excel files

## Environment Variables

### API Service

- `ASPNETCORE_ENVIRONMENT`: Production
- `ASPNETCORE_URLS`: http://+:8080
- `ConnectionStrings__logistics`: PostgreSQL connection string
- `Tokens__Issuer`: JWT issuer
- `Tokens__Key`: JWT signing key

### Frontend Service

- `API_URL`: Backend API URL (http://api:8080)

## Troubleshooting

### Database Connection Issues

1. Ensure PostgreSQL container is healthy:
   ```bash
   docker-compose -f docker-compose-new.yml ps
   ```

2. Check PostgreSQL logs:
   ```bash
   docker-compose -f docker-compose-new.yml logs db
   ```

### API Not Starting

1. Check if database is ready (API waits for DB health check)
2. Verify connection string in environment variables
3. Check API logs for errors

### Frontend Cannot Connect to API

1. Verify API is running: http://localhost:8080
2. Check nginx configuration in `frontend/nginx.conf`
3. Ensure API service name is correct in docker-compose network

## Production Considerations

For production deployment, consider:

1. **Security**:
   - Change default passwords
   - Use Docker secrets for sensitive data
   - Enable HTTPS/SSL
   - Configure proper CORS policies

2. **Performance**:
   - Adjust PostgreSQL configuration
   - Configure connection pooling
   - Add Redis for caching
   - Use production-grade reverse proxy

3. **Monitoring**:
   - Add health check endpoints
   - Integrate logging/monitoring tools
   - Configure backup strategy for database

## Switching Back to SQL Server

If you need to switch back to SQL Server:

1. Update `docker-compose-new.yml` to use SQL Server image
2. Change connection string in `appsettings.json`
3. Update `Program.cs` to use `SqlServerProvider` instead of `PostgreSqlProvider`
4. Update `CoreAPI.csproj` to use SQL Server packages

## License

[Your License Here]
