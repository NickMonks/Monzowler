# Runtime base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

# Install Chromium & dependencies
USER root
RUN apt-get update && apt-get install -y --no-install-recommends \
    chromium \
    chromium-driver \
    fonts-liberation \
    libnss3 \
    libatk-bridge2.0-0 \
    libxss1 \
    libasound2 \
    libgbm1 \
    libgtk-3-0 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/api/Monzowler.CLI/Monzowler.CLI.csproj", "src/api/Monzowler.CLI/"]
COPY ["src/core/Monzowler.Application/Monzowler.Application.csproj", "src/core/Monzowler.Application/"]
COPY ["src/core/Monzowler.Domain/Monzowler.Domain.csproj", "src/core/Monzowler.Domain/"]
COPY ["src/core/Monzowler.Shared/Monzowler.Shared.csproj", "src/core/Monzowler.Shared/"]
COPY ["src/infrastructure/Monzowler.HttpClient/Monzowler.HttpClient.csproj", "src/infrastructure/Monzowler.HttpClient/"]
COPY ["src/infrastructure/Monzowler.Persistence/Monzowler.Persistence.csproj", "src/infrastructure/Monzowler.Persistence/"]

RUN dotnet restore "src/api/Monzowler.CLI/Monzowler.CLI.csproj"

COPY . .

WORKDIR "src/api/Monzowler.CLI"
RUN dotnet build "./Monzowler.CLI.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Monzowler.CLI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Monzowler.CLI.dll"]
