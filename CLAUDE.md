# CLAUDE.md

This file provides guidance to Claude Code when working with code in this repository.

## Project Overview

Madtorio is a Blazor Server web application (.NET 10) for managing Factorio game save files. It provides user authentication with admin roles, file management for uploads/downloads, and an admin-editable rules system.

## Documentation Structure

Detailed documentation is organized in the [docs/](docs/) directory:

- **[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)** - System architecture, tech stack, services, database design
- **[docs/DEVELOPMENT.md](docs/DEVELOPMENT.md)** - Development setup, build commands, testing, git workflow
- **[docs/FEATURES.md](docs/FEATURES.md)** - Feature documentation (Rules System, Save Files, Statistics)
- **[docs/DEPLOYMENT.md](docs/DEPLOYMENT.md)** - Deployment guides, data storage, Docker, Unraid setup

## Quick Reference

### Key Directories

```
Madtorio/
├── Components/         # Blazor components (Pages, Layouts, Shared)
│   ├── Pages/         # Routes (public and /Admin pages)
│   └── Account/       # Authentication UI
├── Data/              # Database layer (Models, Migrations, Seed)
├── data/              # Runtime data (uploads, database, keys) - git-ignored
├── Services/          # Business logic (interface-based DI)
├── Middleware/        # Custom middleware
├── Controllers/       # API controllers
└── Madtorio.Tests/    # Test project
```

**Important**: `Data/` (uppercase) = source code, `data/` (lowercase) = runtime data

### Common Commands

```bash
# Build and run
dotnet build
dotnet run                    # Development mode
dotnet watch                  # With hot reload

# Database migrations
dotnet ef database update
dotnet ef migrations add <Name> --output-dir Data/Migrations

# Testing
dotnet test
```

### Core Services

- `ISaveFileService` - Save file metadata CRUD
- `IFileStorageService` - Physical file storage operations
- `IChunkedFileUploadService` - Chunked file upload handling
- `IRulesService` - Rules and server configuration management
- `IStatisticsService` - Usage tracking and metrics

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for detailed service documentation.

## Development Guidelines

### Git Workflow

1. **Create feature branch** from `main`:
   ```bash
   git checkout -b feature/feature-name
   ```

2. **Make changes** with tests

3. **Commit** using [Conventional Commits](https://www.conventionalcommits.org/):
   ```bash
   git commit -m "feat: Add feature description"
   git commit -m "fix: Fix bug description"
   git commit -m "docs: Update documentation"
   ```

4. **Create PR** to `main` and wait for CI checks

5. **Only commit when a feature is complete and tested**

### Testing Requirements

- Implement tests for all new features and functionality
- Tests must pass before committing: `dotnet test`
- When creating new components, add them to `/admin/component-test` page

### Documentation Requirements

When making significant changes, update relevant documentation:

- Architecture changes → [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)
- New features → [docs/FEATURES.md](docs/FEATURES.md)
- Deployment changes → [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md)
- Build/dev changes → [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md)

## Architecture Patterns

### Service Layer Pattern

All business logic follows interface-based dependency injection:

```csharp
// Define interface
public interface IMyService { }

// Implement service
public class MyService : IMyService { }

// Register in Program.cs
builder.Services.AddScoped<IMyService, MyService>();
```

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for complete service documentation.

### Authentication & Authorization

- ASP.NET Core Identity with role-based authorization
- "Admin" role for administrative functions
- Admin pages protected with `[Authorize(Policy = "Admin")]`
- Default admin credentials seeded on first run

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for security details.

### Database

- SQLite with Entity Framework Core
- Auto-migrates and seeds data on startup via `DbInitializer`
- Soft deletes using `IsEnabled` flag where applicable
- WAL mode enabled for better concurrency

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for schema details.

## Features

### Rules System

Admin-editable database-driven community rules organized by categories.

- Categories and rules with soft deletes (`IsEnabled`)
- Display order controlled by admin
- Server configuration key-value storage
- Public display at `/rules`, admin management at `/admin/rules`

See [docs/FEATURES.md](docs/FEATURES.md) for complete Rules System documentation.

### Save File Management

Upload, store, and download Factorio save files (up to 500MB).

- Chunked uploads for large files
- Temporary storage in `data/uploads/temp/`
- Final storage in `data/uploads/saves/`
- Metadata tracked in database

See [docs/FEATURES.md](docs/FEATURES.md) for file management details.

### Statistics Tracking

Usage metrics and admin dashboard analytics.

- Tracks uploads, downloads, deletions, logins
- Admin dashboard at `/admin/statistics`
- Event history and trend analysis

See [docs/FEATURES.md](docs/FEATURES.md) for statistics documentation.

## Data Storage

### Development
- Database: `madtorio.db` (project root)
- Uploads: `data/uploads/saves/`
- Keys: `data/keys/`

### Production/Docker
- All data consolidated in: `/app/data/`
- Database: `/app/data/madtorio.db`
- Uploads: `/app/data/uploads/saves/`
- Keys: `/app/data/keys/`

### Important Notes
- **Always mount `/app/data` volume in Docker** to persist data
- Without volume mount, all data is ephemeral and lost on restart
- `Data/` (uppercase) = source code, `data/` (lowercase) = runtime storage

See [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md) for deployment and backup strategies.

## Additional Resources

- [README.md](README.md) - Project overview and quick start
- [README.Docker.md](README.Docker.md) - Comprehensive Docker deployment guide
- [CICD-SETUP.md](CICD-SETUP.md) - CI/CD pipeline configuration
- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) - System architecture
- [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md) - Development guide
- [docs/FEATURES.md](docs/FEATURES.md) - Feature documentation
- [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md) - Deployment guide

## Support

For issues and questions:
- GitHub Issues: https://github.com/helpower2/Madtorio/issues
- Check relevant documentation in [docs/](docs/) directory
