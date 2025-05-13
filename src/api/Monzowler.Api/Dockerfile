FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

# Install Chrome
USER root

# Install Chrome and ChromeDriver
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

USER $APP_UID

WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/api/Monzowler.Api/Monzowler.Api.csproj", "src/api/Monzowler.Api/"]
COPY ["src/core/Monzowler.Application/Monzowler.Application.csproj", "src/core/Monzowler.Application/"]
COPY ["src/core/Monzowler.Domain/Monzowler.Domain.csproj", "src/core/Monzowler.Domain/"]
COPY ["src/core/Monzowler.Shared/Monzowler.Shared.csproj", "src/core/Monzowler.Shared/"]
COPY ["src/infrastructure/Monzowler.HttpClient/Monzowler.HttpClient.csproj", "src/infrastructure/Monzowler.HttpClient/"]
COPY ["src/infrastructure/Monzowler.Persistence/Monzowler.Persistence.csproj", "src/infrastructure/Monzowler.Persistence/"]

RUN dotnet restore "src/api/Monzowler.Api/Monzowler.Api.csproj"
COPY . .
WORKDIR "src/api/Monzowler.Api"
RUN dotnet build "./Monzowler.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Monzowler.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Monzowler.Api.dll"]
