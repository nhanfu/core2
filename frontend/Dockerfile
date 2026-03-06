FROM oven/bun:1-alpine

WORKDIR /app

# Copy package files first to leverage Docker cache
COPY package.json bun.lockb* ./

# Install dependencies using bun
RUN bun install

# Copy source code
COPY . .

# Expose port Vite uses
EXPOSE 5173

# Command to run Vite with host flag to ensure it listens on all interfaces
CMD ["bun", "run", "dev", "--", "--host", "0.0.0.0"]
