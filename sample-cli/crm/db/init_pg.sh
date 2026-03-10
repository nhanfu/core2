#!/bin/bash

# PostgreSQL Database Initialization Script
# Waits for PostgreSQL to be ready and runs initialization scripts

set -e

echo "Waiting for PostgreSQL to be ready..."
echo "POSTGRES_HOST=$POSTGRES_HOST"
echo "POSTGRES_DB=$POSTGRES_DB"

# Wait for PostgreSQL to be ready
until PGPASSWORD="$POSTGRES_PASSWORD" psql -h "$POSTGRES_HOST" -U "$POSTGRES_USER" -d "$POSTGRES_DB" -c '\q'; do
  sleep 5
  echo "Still waiting for PostgreSQL..."
done

echo "PostgreSQL is ready. Running initialization scripts..."

# Run schema script first
if [ -f "/scripts/202511221_schema_pg.sql" ]; then
  echo "Running schema script..."
  PGPASSWORD="$POSTGRES_PASSWORD" psql -h "$POSTGRES_HOST" -U "$POSTGRES_USER" -d "$POSTGRES_DB" -f "/scripts/202511221_schema_pg.sql"
fi

# Run seed script
if [ -f "/scripts/202511222_seed_pg.sql" ]; then
  echo "Running seed script..."
  PGPASSWORD="$POSTGRES_PASSWORD" psql -h "$POSTGRES_HOST" -U "$POSTGRES_USER" -d "$POSTGRES_DB" -f "/scripts/202511222_seed_pg.sql"
fi

echo "Database initialization complete."
