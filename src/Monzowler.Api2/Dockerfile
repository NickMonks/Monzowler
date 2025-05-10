FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Monzowler.Api2/Monzowler.Api2.csproj", "src/Monzowler.Api2/"]
RUN dotnet restore "src/Monzowler.Api2/Monzowler.Api2.csproj"
COPY . .
WORKDIR "/src/Monzowler.Api2"
RUN dotnet build "./Monzowler.Api2.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Monzowler.Api2.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM public.ecr.aws/lambda/dotnet:8 AS final
WORKDIR /var/task
COPY --from=publish /app/publish .
CMD [ "Monzowler.Api2::Monzowler.Api2.LambdaEntryPoint::FunctionHandlerAsync" ]