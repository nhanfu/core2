# Quick Command Reference

## Setup Commands

```powershell
# 1. Restore packages
cd CoreAPI
dotnet restore
cd ..

# 2. Build and start all services
docker-compose -f docker-compose-new.yml up -d --build

# 3. View logs
docker-compose -f docker-compose-new.yml logs -f
```

## Daily Use Commands

```powershell
# Start services
docker-compose -f docker-compose-new.yml up -d

# Stop services
docker-compose -f docker-compose-new.yml down

# Restart specific service
docker-compose -f docker-compose-new.yml restart api
docker-compose -f docker-compose-new.yml restart app
docker-compose -f docker-compose-new.yml restart db

# View logs
docker-compose -f docker-compose-new.yml logs -f api
docker-compose -f docker-compose-new.yml logs -f app
docker-compose -f docker-compose-new.yml logs -f db

# Rebuild and restart a service
docker-compose -f docker-compose-new.yml up -d --build api
```

## Database Commands

```powershell
# Connect to PostgreSQL
docker exec -it core2-postgres psql -U postgres -d crm

# Backup database
docker exec core2-postgres pg_dump -U postgres crm > backup.sql

# Restore database
docker exec -i core2-postgres psql -U postgres -d crm < backup.sql

# View database logs
docker-compose -f docker-compose-new.yml logs db
```

## Debugging Commands

```powershell
# Execute shell in API container
docker exec -it core2-api bash

# Execute shell in frontend container
docker exec -it core2-frontend sh

# Execute shell in PostgreSQL container
docker exec -it core2-postgres bash

# Check service status
docker-compose -f docker-compose-new.yml ps

# View resource usage
docker stats core2-api core2-frontend core2-postgres
```

## Cleanup Commands

```powershell
# Stop and remove containers
docker-compose -f docker-compose-new.yml down

# Stop and remove containers + volumes (DELETES DATA!)
docker-compose -f docker-compose-new.yml down -v

# Remove all stopped containers
docker container prune

# Remove unused images
docker image prune

# Remove unused volumes
docker volume prune
```

## Testing Commands

```powershell
# Test API health
curl http://localhost:8080/api/health

# Test API endpoint
curl http://localhost:8080/api/your-endpoint

# Test frontend
start http://localhost

# Check database connection
docker exec core2-postgres pg_isready -U postgres
```

## Build Commands

```powershell
# Build frontend only
cd frontend
npm run build1
cd ..

# Build API only
cd CoreAPI
dotnet build
cd ..

# Rebuild Docker images
docker-compose -f docker-compose-new.yml build --no-cache
```
