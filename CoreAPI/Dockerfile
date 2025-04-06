FROM mcr.microsoft.com/dotnet/sdk:9.0

# Install debugging tools
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
    unzip \
    procps \
    curl \
    && curl -sSL https://aka.ms/getvsdbgsh | bash /dev/stdin -v latest -l /remote_debugger \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copy project file and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy source code
COPY . ./

# Environment variables
ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_URLS=http://+:80
ENV DOTNET_USE_POLLING_FILE_WATCHER=1

# Expose ports
EXPOSE 80
EXPOSE 2222

# Entrypoint for debugging or running
ENTRYPOINT ["dotnet", "watch", "run", "--urls=http://+:80"]