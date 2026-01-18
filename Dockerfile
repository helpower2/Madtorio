# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies (layer caching)
COPY ["Madtorio.csproj", "./"]
RUN dotnet restore "Madtorio.csproj"

# Copy remaining source files
COPY . .

# Build and publish in Release configuration
ARG VERSION=dev
RUN dotnet publish "Madtorio.csproj" -c Release -o /app/publish --no-restore -p:Version=${VERSION} -p:InformationalVersion=${VERSION}

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
ARG VERSION=dev
WORKDIR /app

# Create AppData directories for volumes (runtime data storage)
RUN mkdir -p /app/AppData/uploads/saves /app/AppData/uploads/temp /app/AppData/keys

# Copy published application
COPY --from=build /app/publish .

# Note: Running as root for compatibility with mounted volumes on systems like Unraid
# The application will create necessary directories at runtime

# Expose port
EXPOSE 8567

# Set environment variables
ENV ASPNETCORE_HTTP_PORTS=8567 \
    ASPNETCORE_ENVIRONMENT=Production \
    APP_VERSION=${VERSION}

# Add metadata labels
LABEL org.opencontainers.image.title="Madtorio" \
      org.opencontainers.image.description="Blazor Server application for managing Factorio save files" \
      org.opencontainers.image.version="${VERSION}" \
      org.opencontainers.image.vendor="helpower2"

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8567/ || exit 1

# Entry point
ENTRYPOINT ["dotnet", "Madtorio.dll"]
