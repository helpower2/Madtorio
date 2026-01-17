# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG VERSION=dev
WORKDIR /src

# Copy csproj and restore dependencies (layer caching)
COPY ["Madtorio.csproj", "./"]
RUN dotnet restore "Madtorio.csproj"

# Copy remaining source files
COPY . .

# Build and publish in Release configuration
RUN dotnet publish "Madtorio.csproj" -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
ARG VERSION=dev
WORKDIR /app

# Create data directories for volumes (using built-in 'app' user from .NET base image)
RUN mkdir -p /app/data/uploads/saves /app/data/uploads/temp /app/data/keys && \
    chown -R app:app /app/data

# Copy published application
COPY --from=build /app/publish .

# Change ownership of app files to built-in 'app' user
RUN chown -R app:app /app

# Switch to non-root user (built-in 'app' user, UID 1654 in .NET images)
USER app

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    APP_VERSION=${VERSION}

# Add metadata labels
LABEL org.opencontainers.image.title="Madtorio" \
      org.opencontainers.image.description="Blazor Server application for managing Factorio save files" \
      org.opencontainers.image.version="${VERSION}" \
      org.opencontainers.image.vendor="helpower2"

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/ || exit 1

# Entry point
ENTRYPOINT ["dotnet", "Madtorio.dll"]
