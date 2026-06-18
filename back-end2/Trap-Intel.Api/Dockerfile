# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Install curl for health checks
USER root
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
USER $APP_UID

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy all project files for restore
COPY ["Trap-Intel.Api/Trap-Intel.Api.csproj", "Trap-Intel.Api/"]
COPY ["Trap-Intel.Infrastructure/Trap-Intel.Infrastructure.csproj", "Trap-Intel.Infrastructure/"]
COPY ["Trap-Intel.Domain/Trap-Intel.Domain.csproj", "Trap-Intel.Domain/"]

RUN dotnet restore "./Trap-Intel.Api/Trap-Intel.Api.csproj"
COPY . .
WORKDIR "/src/Trap-Intel.Api"
RUN dotnet build "./Trap-Intel.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Trap-Intel.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

USER root
RUN mkdir -p /app/wwwroot/media && chown -R $APP_UID:$APP_UID /app
USER $APP_UID

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Trap-Intel.Api.dll"]