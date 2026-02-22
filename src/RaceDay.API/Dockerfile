# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
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
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Install curl, create non-root user, copy files, and set permissions
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/* && \
    useradd -m -u 1000 -s /bin/bash appuser && \
    chown -R appuser:appuser /app

# Copy published files with correct ownership
COPY --chown=appuser:appuser --from=publish /app/publish .

# Remove write permissions to prevent tampering
RUN chmod -R a-w /app

USER appuser

# Set environment variables for Azure
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "RaceDay.API.dll"]
