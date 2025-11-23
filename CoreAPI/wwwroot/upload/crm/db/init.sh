#!/bin/bash

# Wait for SQL Server to be ready
echo "Waiting for SQL Server to be ready..."
echo "SA_PASSWORD=$SA_PASSWORD"
until sqlcmd -S sqlserver -U sa -P "$SA_PASSWORD" -Q "SELECT 1"; do
  sleep 5
  echo "Still waiting..."
done
set -e

echo "SQL Server is ready. Running initialization scripts..."

# Run all .sql files in the scripts directory
for sql_file in /scripts/*.sql; do
  if [ -f "$sql_file" ]; then
    echo "Running $sql_file..."
    sqlcmd -S sqlserver -U sa -P "$SA_PASSWORD" -i "$sql_file"
  fi
done

echo "Database initialization complete."
