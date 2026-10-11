# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files and restore (cached layer)
COPY ["src/RaceDay.API/RaceDay.API.csproj", "src/RaceDay.API/"]
COPY ["src/RaceDay.Core/RaceDay.Core.csproj", "src/RaceDay.Core/"]
RUN dotnet restore "src/RaceDay.API/RaceDay.API.csproj"

# Copy the rest of the source code and publish (publish builds)
COPY src/RaceDay.API/ src/RaceDay.API/
COPY src/RaceDay.Core/ src/RaceDay.Core/
WORKDIR /src/src/RaceDay.API
RUN dotnet publish "RaceDay.API.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# curl is used by the docker-compose healthcheck (GET /health); create non-root user
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/* && \
    useradd -m -u 1001 -s /bin/bash appuser && \
    chown -R appuser:appuser /app

# Copy published files with correct ownership
COPY --chown=appuser:appuser --from=build /app/publish .

# Remove write permissions to prevent tampering (logs go to the console only)
RUN chmod -R a-w /app

USER appuser

# Listen on 8080. On Railway the injected PORT variable takes precedence (see Program.cs).
ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "RaceDay.API.dll"]
