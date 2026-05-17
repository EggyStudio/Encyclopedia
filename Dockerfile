# Multi-stage Dockerfile for the Encyclopedia Blazor app (net10).
#
# Build:  docker build -t encyclopedia .
# Run:    docker run --rm -p 8080:8080 -e ConnectionStrings__Postgres=... encyclopedia

# ---- Build stage --------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Restore first so layer caches across source edits.
COPY Encyclopedia.csproj NuGet.config* ./
RUN dotnet restore Encyclopedia.csproj

COPY . .
RUN dotnet publish Encyclopedia.csproj -c Release -o /app/publish \
    /p:UseAppHost=false /p:PublishTrimmed=false

# ---- Runtime stage ------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app

# Curl for healthchecks.
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_NOLOGO=1

EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
  CMD curl -fsS http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Encyclopedia.dll"]
