# syntax=docker/dockerfile:1

# ---- Build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so this layer stays cached until a project file changes.
COPY Directory.Build.props Directory.Packages.props ./
COPY ECommerce.Api/ECommerce.Api.csproj ECommerce.Api/
COPY ECommerce.Application/ECommerce.Application.csproj ECommerce.Application/
COPY ECommerce.Domain/ECommerce.Domain.csproj ECommerce.Domain/
COPY ECommerce.Domain.Shared/ECommerce.Domain.Shared.csproj ECommerce.Domain.Shared/
COPY ECommerce.Infrastructure/ECommerce.Infrastructure.csproj ECommerce.Infrastructure/
RUN dotnet restore ECommerce.Api/ECommerce.Api.csproj

COPY . .
RUN dotnet publish ECommerce.Api/ECommerce.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---- Runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# curl is only used by the docker compose health check.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID

ENTRYPOINT ["dotnet", "ECommerce.Api.dll"]
