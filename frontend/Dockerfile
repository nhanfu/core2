FROM node:18-alpine

RUN npm install -g pnpm

WORKDIR /app

# Copy package files first to leverage Docker cache
COPY package.json pnpm-lock.yaml* ./
RUN pnpm install

# Copy source code
COPY . .

# Expose port Vite uses
EXPOSE 5173

# Command to run Vite with host flag to ensure it listens on all interfaces
CMD ["pnpm", "run", "dev", "--", "--host", "0.0.0.0"]
