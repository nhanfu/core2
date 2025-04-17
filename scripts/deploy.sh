#!/bin/bash
#!/bin/bash
set -a
[ -f .env ] && . .env
set +a

set -e

echo "Logging into Docker Hub..."
echo "$DOCKER_PASSWORD" | docker login -u "$DOCKER_USERNAME" --password-stdin

echo "Pulling Docker images..."
docker pull $DOCKER_USERNAME/corejs-coreapi:${IMAGE_TAG}
docker pull $DOCKER_USERNAME/corejs-frontend:${IMAGE_TAG}

echo "Pruning unused Docker images not used for 4 hours..."
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
    image: $DOCKER_USERNAME/corejs-coreapi:${IMAGE_TAG}
    ports:
      - "8080:80"
      - "2222:2222"
    restart: unless-stopped

  frontend:
    image: $DOCKER_USERNAME/corejs-frontend:${IMAGE_TAG}
    ports:
      - "5173:5173"
    restart: unless-stopped
COMPOSE
fi

echo "Starting the application..."
docker compose down
docker compose up -d

# Save deployment info to a file
echo "Deployment completed on $(date)" > deployment_info.txt
echo "Images:" >> deployment_info.txt
echo "CoreAPI: $DOCKER_USERNAME/corejs-coreapi:${IMAGE_TAG}" >> deployment_info.txt
echo "Frontend: $DOCKER_USERNAME/corejs-frontend:${IMAGE_TAG}" >> deployment_info.txt