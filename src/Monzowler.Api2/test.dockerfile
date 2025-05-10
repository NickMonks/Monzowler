FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build

ENV PATH $PATH:/root/.dotnet/tools
RUN dotnet tool install -g Amazon.Lambda.Tools

RUN apk add --update docker openrc zip py-pip
RUN pip install awscli --break-system-packages

WORKDIR /
COPY ./src/ ./src

WORKDIR /src/Monzowler.Api2
RUN dotnet lambda package --output-package lambda-package.zip

RUN zip -ur lambda-package.zip ./appsettings.json