# Use the official .NET SDK image as the build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set working directory
WORKDIR /app

# Use build arguments to control what gets copied or not
ARG SKIP_RESTORE=false
ARG SKIP_BUILD=false
ARG SKIP_PUBLISH=false

# Copy just the solution and project files for restore
COPY Core.sln .
COPY CoreAPI/*.csproj ./CoreAPI/
COPY Core/*.csproj ./Core/

# Restore dependencies if not skipped
RUN if [ "$SKIP_RESTORE" = "false" ]; then \
        dotnet restore; \
    fi

# Copy the rest of the source code
COPY . .

# Build if not skipped
RUN if [ "$SKIP_BUILD" = "false" ]; then \
        cd CoreAPI && \
        dotnet build -c Release -o /app/build; \
    fi

# Publish if not skipped
RUN if [ "$SKIP_PUBLISH" = "false" ]; then \
        cd CoreAPI && \
        dotnet publish -c Release -o /app/publish; \
    fi

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CoreAPI.dll"]