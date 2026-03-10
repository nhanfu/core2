#!/bin/bash

# PostgreSQL Local Setup Script
# Installs PostgreSQL and initializes the CRM database

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
POSTGRES_PASSWORD="YourStrongPassword123!"

echo "=== PostgreSQL Local Setup ==="

# Check if PostgreSQL is installed
if ! command -v psql &> /dev/null; then
    echo "Installing PostgreSQL..."
    sudo apt update
    sudo apt install -y postgresql postgresql-contrib
fi

# Start PostgreSQL service
echo "Starting PostgreSQL service..."
sudo systemctl start postgresql 2>/dev/null || true
sudo systemctl enable postgresql 2>/dev/null || true

# Wait for PostgreSQL to be ready
echo "Waiting for PostgreSQL to be ready..."
until sudo -u postgres psql -c '\q' 2>/dev/null; do
    sleep 2
done
echo "PostgreSQL is ready."

# Create database and set password
echo "Setting up database..."
sudo -u postgres psql -c "ALTER USER postgres WITH PASSWORD '$POSTGRES_PASSWORD';" 2>/dev/null || true
sudo -u postgres psql -c "DROP DATABASE IF EXISTS crm;" 2>/dev/null || true
sudo -u postgres psql -c "CREATE DATABASE crm;"

# Run initialization scripts
echo "Running schema script..."
PGPASSWORD="$POSTGRES_PASSWORD" psql -h localhost -U postgres -d crm -f "$SCRIPT_DIR/202511221_schema_pg.sql"

echo "Running seed script..."
PGPASSWORD="$POSTGRES_PASSWORD" psql -h localhost -U postgres -d crm -f "$SCRIPT_DIR/202511222_seed_pg.sql"

echo "=== Setup complete! ==="
echo "Database: crm"
echo "User: postgres"
echo "Password: $POSTGRES_PASSWORD"
echo "Connection: postgresql://postgres:$POSTGRES_PASSWORD@localhost:5432/crm"
