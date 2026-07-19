# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files for caching
COPY ["ecommerce.aspnet.slnx", "./"]
COPY ["Ecommerce.Api/ECommerce.Api.csproj", "Ecommerce.Api/"]
COPY ["ECommerce.Application/ECommerce.Application.csproj", "ECommerce.Application/"]
COPY ["Ecommerce.Domain/ECommerce.Domain.csproj", "Ecommerce.Domain/"]
COPY ["ECommerce.Domain.Shared/ECommerce.Domain.Shared.csproj", "ECommerce.Domain.Shared/"]
COPY ["ECommerce.Infrastructure/ECommerce.Infrastructure.csproj", "ECommerce.Infrastructure/"]

RUN dotnet restore "Ecommerce.Api/ECommerce.Api.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/Ecommerce.Api"
RUN dotnet publish "ECommerce.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install curl for docker-compose healthcheck
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "ECommerce.Api.dll"]
