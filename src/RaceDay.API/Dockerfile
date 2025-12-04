# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/RaceDay.API/RaceDay.API.csproj", "src/RaceDay.API/"]
COPY ["src/RaceDay.Core/RaceDay.Core.csproj", "src/RaceDay.Core/"]

# Restore dependencies
RUN dotnet restore "src/RaceDay.API/RaceDay.API.csproj"

# Copy the rest of the source code
COPY src/RaceDay.API/ src/RaceDay.API/
COPY src/RaceDay.Core/ src/RaceDay.Core/

# Build the application
WORKDIR /src/src/RaceDay.API
RUN dotnet build "RaceDay.API.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "RaceDay.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Install curl for healthcheck
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

# Create a non-root user to run the application
RUN useradd -m -u 1000 -s /bin/bash appuser && \
    chown -R appuser:appuser /app

# Copy published files
COPY --from=publish /app/publish .

# Set ownership of the application files
RUN chown -R appuser:appuser /app

USER appuser

# Set environment variables for Azure
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "RaceDay.API.dll"]
