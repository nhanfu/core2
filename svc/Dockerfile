FROM denoland/deno:latest

WORKDIR /app

# Copy all source code
COPY . .

# Expose port
EXPOSE 8000

# Run the Deno server
CMD ["run", "--allow-all", "main.ts"]