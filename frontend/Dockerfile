FROM denoland/deno:latest

WORKDIR /app

# Copy dependency files first to leverage Docker cache
COPY deno.json deno.lock ./

# Copy source code
COPY . .

# Expose port Vite uses
EXPOSE 5173

# Run Vite dev server with host flag to ensure it listens on all interfaces
CMD ["deno", "run", "-A", "npm:vite", "--host", "0.0.0.0"]
