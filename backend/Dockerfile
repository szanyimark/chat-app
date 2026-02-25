# Development stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS development
WORKDIR /app

# Copy project files and restore dependencies
COPY ChatApp.Backend.csproj ./ChatApp.Backend.csproj
RUN dotnet restore ChatApp.Backend.csproj

# Copy all source files
COPY . .

# Expose ports for development
EXPOSE 5000 5001

# Run in development mode with hot-reload
ENTRYPOINT ["dotnet", "watch", "run", "--project", "ChatApp.Backend.csproj", "--urls", "http://+:5000"]

# Build stage (for production)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ChatApp.Backend.csproj ./ChatApp.Backend.csproj
RUN dotnet restore ChatApp.Backend.csproj

COPY . .
RUN dotnet build ChatApp.Backend.csproj -c Release -o /app/build
RUN dotnet publish ChatApp.Backend.csproj -c Release -o /app/publish

# Runtime stage (for production)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install Node.js for Angular frontend build
RUN apt-get update && apt-get install -y curl && \
    curl -fsSL https://deb.nodesource.com/setup_20.x | bash - && \
    apt-get install -y nodejs && \
    rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:5000

EXPOSE 5000

ENTRYPOINT ["dotnet", "ChatApp.Backend.dll"]