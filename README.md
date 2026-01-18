# Madtorio

[![CI - Feature Branch](https://github.com/helpower2/Madtorio/actions/workflows/ci-feature.yml/badge.svg)](https://github.com/helpower2/Madtorio/actions/workflows/ci-feature.yml)
[![CD - Main Branch](https://github.com/helpower2/Madtorio/actions/workflows/cd-main.yml/badge.svg)](https://github.com/helpower2/Madtorio/actions/workflows/cd-main.yml)
[![Docker Image](https://img.shields.io/docker/v/helpower2/madtorio?label=Docker&logo=docker)](https://hub.docker.com/r/helpower2/madtorio)
[![License](https://img.shields.io/github/license/helpower2/Madtorio)](LICENSE)

Madtorio is a Blazor Server web application (.NET 10) for managing Factorio game save files. It provides user authentication with admin roles, a file management system for uploading, storing, and downloading save files, and an admin-editable rules system.

## Features

- **User Authentication**: ASP.NET Core Identity with role-based authorization
- **Save File Management**: Upload, store, and download Factorio save files (up to 500MB)
- **Chunked Uploads**: Efficient handling of large files
- **Admin Dashboard**: Manage users, rules, and server configuration
- **Rules System**: Database-driven community rules organized by categories
- **Server Configuration**: Configurable Factorio server IP and settings
- **Docker Ready**: Production-ready Docker configuration for easy deployment

## Quick Start

### Docker (Recommended)

```bash
# Clone the repository
git clone https://github.com/helpower2/Madtorio.git
cd Madtorio

# Create environment file
cp .env.template .env
# Edit .env with your preferred admin credentials

# Start with Docker Compose
docker compose up -d

# Access the application
open http://localhost:8083
```

Default admin credentials (change in `.env`):
- Email: `admin@madtorio.com`
- Password: `Madtorio2026!`

### Local Development

```bash
# Prerequisites: .NET 10 SDK

# Restore dependencies
dotnet restore

# Run the application
dotnet run

# Or with hot reload
dotnet watch

# Access the application
open http://localhost:5000
```

## CI/CD Pipeline

This project uses GitHub Actions for continuous integration and deployment:

### Workflows

- **Feature Branch CI** (`ci-feature.yml`): Runs on all branches except main
  - Builds and tests the project
  - Fast feedback on code quality
  - Required to pass before merging to main

- **Main Branch CD** (`cd-main.yml`): Runs on main branch and version tags
  - Builds multi-platform Docker images (linux/amd64, linux/arm64)
  - Pushes to Docker Hub private repository
  - Creates GitHub releases for version tags
  - Tags: `latest`, `stable`, `v1.0.0`, etc.

- **PR Checks** (`pr-checks.yml`): Runs on pull requests
  - Comprehensive validation
  - Code formatting checks
  - Automated PR comments

### Branch Strategy

- **main**: Protected production branch
- **feature/***: Feature development branches
- **hotfix/***: Urgent production fixes

### Deployment

Images are pushed to Docker Hub: `helpower2/madtorio`

**On Unraid** (auto-updates daily at 01:00):
```bash
cd /mnt/user/appAppData/madtorio
docker compose pull
docker compose up -d
```

**Manual deployment**:
```bash
docker pull helpower2/madtorio:latest
docker compose up -d --force-recreate
```

### Creating a Release

```bash
# Tag the current commit
git tag -a v1.0.0 -m "Release version 1.0.0"
git push origin v1.0.0

# GitHub Actions will:
# - Build and push Docker images with version tags
# - Create a GitHub release with changelog
# - Tag Docker images: v1.0.0, v1.0, v1, latest, stable
```

## Development

### Project Structure

```
Madtorio/
├── Components/          # Razor components
│   ├── Pages/          # Page components (routes)
│   ├── Layout/         # Layout components
│   └── Account/        # Authentication components
├── Data/               # Database layer
│   ├── Models/         # Entity models
│   ├── Migrations/     # EF Core migrations
│   └── Seed/           # Database seeding
├── Services/           # Business logic services
├── Madtorio.Tests/     # Test project
└── .github/            # CI/CD workflows
```

### Key Services

- **ISaveFileService**: CRUD operations for save file metadata
- **IFileStorageService**: Physical file storage operations
- **IChunkedFileUploadService**: Chunked file upload handling
- **IRulesService**: Rules and server configuration management

### Database

- SQLite database (`AppData/madtorio.db`)
- Auto-migrates on startup
- Seeds default admin user and rules

### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Database Migrations

```bash
# Add a new migration
dotnet ef migrations add MigrationName --output-dir Data/Migrations

# Apply migrations
dotnet ef database update

# Revert last migration
dotnet ef migrations remove
```

## Configuration

### Environment Variables

- `ASPNETCORE_ENVIRONMENT`: Application environment (Development/Production)
- `AdminEmail`: Default admin email address
- `AdminPassword`: Default admin password (min 8 chars, requires uppercase, lowercase, digit, special char)
- `ConnectionStrings__DefaultConnection`: SQLite database connection string

### appsettings.json

Configuration files by environment:
- `appsettings.json`: Base configuration
- `appsettings.Development.json`: Development overrides
- `appsettings.Production.json`: Production overrides

## Docker

### Building Locally

```bash
# Build image
docker build -t madtorio:local .

# Run container
docker run -d \
  -p 8083:8080 \
  -v $(pwd)/data:/app/data \
  -e AdminEmail=admin@example.com \
  -e AdminPassword=YourSecurePassword123! \
  madtorio:local
```

### Multi-platform Build

```bash
# Build for multiple architectures
docker buildx build \
  --platform linux/amd64,linux/arm64 \
  -t helpower2/madtorio:latest \
  --push .
```

## Unraid Deployment

See [README.Docker.md](README.Docker.md) for comprehensive Unraid installation instructions, including:
- Docker Compose installation
- Docker Template configuration
- Volume management
- Backup and restore procedures
- Troubleshooting guide

## Security

- Non-root container user (UID 1654)
- Secrets managed via GitHub Secrets
- Docker Hub credentials never committed
- Branch protection on main branch
- Automated dependency updates via Dependabot

## Contributing

1. Create a feature branch from main
2. Make your changes
3. Ensure tests pass locally
4. Create a pull request to main
5. Wait for CI checks to pass
6. Request review

## License

[Add your license here]

## Documentation

Comprehensive documentation is available in the [docs/](docs/) directory:

- **[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)** - System architecture, tech stack, services, database design
- **[docs/DEVELOPMENT.md](docs/DEVELOPMENT.md)** - Development setup, build commands, testing, git workflow
- **[docs/FEATURES.md](docs/FEATURES.md)** - Feature documentation (Rules System, Save Files, Statistics)
- **[docs/DEPLOYMENT.md](docs/DEPLOYMENT.md)** - Deployment guides, data storage, Docker, Unraid setup

Additional resources:
- [CLAUDE.md](CLAUDE.md) - Quick reference for Claude Code
- [README.Docker.md](README.Docker.md) - Docker deployment guide
- [CICD-SETUP.md](CICD-SETUP.md) - CI/CD pipeline setup

## Support

For issues and questions:
- GitHub Issues: https://github.com/helpower2/Madtorio/issues
- Documentation: See [docs/](docs/) directory for detailed guides

## Version History

See [Releases](https://github.com/helpower2/Madtorio/releases) for changelog and version history.
