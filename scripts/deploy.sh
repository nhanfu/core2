#!/bin/bash
# Exit on any error
set -e

# Check for required environment variables
required_vars=("DOCKER_USERNAME" "DOCKER_PASSWORD" "IMAGE_TAG" "ALLOWED_HOSTS")
for var in "${required_vars[@]}"; do
  if [ -z "${!var}" ]; then
    echo "Error: Required environment variable $var is not set"
    exit 1
  fi
done

# Create or update the .env file
echo "Updating .env file..."
ENV_FILE=".env"

# Create .env file if it doesn't exist
touch "$ENV_FILE"

# Define the environment variables to update
declare -A env_vars=(
  ["DOCKER_USERNAME"]="$DOCKER_USERNAME"
  ["DOCKER_PASSWORD"]="$DOCKER_PASSWORD"
  ["IMAGE_TAG"]="$IMAGE_TAG"
  ["ALLOWED_HOSTS"]="$ALLOWED_HOSTS"
)

# Update or add each variable in the .env file
for key in "${!env_vars[@]}"; do
  value="${env_vars[$key]}"
  if grep -q "^$key=" "$ENV_FILE"; then
    sed -i "s|^$key=.*|$key=$value|" "$ENV_FILE"
  else
    echo "$key=$value" >> "$ENV_FILE"
  fi
done

echo "Logging into Docker Hub..."
echo "$DOCKER_PASSWORD" | docker login -u "$DOCKER_USERNAME" --password-stdin || {
  echo "Failed to log in to Docker Hub"
  exit 1
}

echo "Pulling Docker images..."
docker pull $DOCKER_USERNAME/corejs-coreapi:latest || {
  echo "Failed to pull CoreAPI image"
  exit 1
}
docker pull $DOCKER_USERNAME/corejs-frontend:latest || {
  echo "Failed to pull Frontend image"
  exit 1
}

echo "Pruning unused Docker images..."
docker image prune -f --filter "until=4h"

echo "Checking if docker-compose.yml exists..."
if [ -f docker-compose.yml ]; then
  echo "docker-compose.yml already exists. Skipping generation..."
else
  echo "docker-compose.yml does not exist. Creating a new one..."
  echo "Generating docker-compose.yml..."
  cat <<COMPOSE > docker-compose.yml
version: '3.8'

services:
  coreapi:
    image: $DOCKER_USERNAME/corejs-coreapi:latest
    ports:
      - "8080:80"
      - "2222:2222"
    restart: unless-stopped

  frontend:
    image: $DOCKER_USERNAME/corejs-frontend:latest
    ports:
      - "5173:5173"
    restart: unless-stopped
COMPOSE
fi

echo "Starting the application..."
docker compose down || echo "No existing containers to stop"
docker compose up -d || {
  echo "Failed to start containers"
  exit 1
}

# Save deployment info to a file
echo "Deployment completed on $(date)" > deployment_info.txt
echo "Images:" >> deployment_info.txt
echo "CoreAPI: $DOCKER_USERNAME/corejs-coreapi:latest" >> deployment_info.txt
echo "Frontend: $DOCKER_USERNAME/corejs-frontend:latest" >> deployment_info.txt

echo "Deployment completed successfully!"