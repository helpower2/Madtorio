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
├── AppData/           # Runtime data (uploads, database, keys) - git-ignored
├── Services/          # Business logic (interface-based DI)
├── Middleware/        # Custom middleware
├── Controllers/       # API controllers
└── Madtorio.Tests/    # Test project
```

**Important**: `Data/` (uppercase) = source code, `AppData/` (lowercase) = runtime data

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
- `IModRequestService` - Community mod request submissions and voting

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

### Security Considerations

- Validate all user input on server-side (never trust client-side validation alone)
- Use `[Authorize]` attributes on all authenticated endpoints
- Admin pages require `[Authorize(Roles = "Admin")]` or `[Authorize(Policy = "Admin")]`
- Sanitize user-generated content to prevent XSS
- Use parameterized queries (EF Core handles this) to prevent SQL injection
- Review OWASP Top 10 when implementing new features

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
- Temporary storage in `AppData/uploads/temp/`
- Final storage in `AppData/uploads/saves/`
- Metadata tracked in database

See [docs/FEATURES.md](docs/FEATURES.md) for file management details.

### Statistics Tracking

Usage metrics and admin dashboard analytics.

- Tracks uploads, downloads, deletions, logins
- Admin dashboard at `/admin/statistics`
- Event history and trend analysis

See [docs/FEATURES.md](docs/FEATURES.md) for statistics documentation.

### Mod Request System

Community-driven mod request and voting system.

- Users submit mod requests at `/request-mod`
- Community voting on requests at `/mod-requests`
- Admin management at `/admin/mod-requests`
- Request status tracking (Pending, Approved, Rejected, Completed)
- Activity logging for audit trail

See [docs/FEATURES.md](docs/FEATURES.md) for Mod Request documentation.

## Data Storage

### Development
- Database: `AppData/madtorio.db`
- Uploads: `AppData/uploads/saves/`
- Keys: `AppData/keys/`

### Production/Docker
- All data consolidated in: `/app/AppData/`
- Database: `/app/AppData/madtorio.db`
- Uploads: `/app/AppData/uploads/saves/`
- Keys: `/app/AppData/keys/`

### Important Notes
- **Always mount `/app/AppData` volume in Docker** to persist data
- Without volume mount, all data is ephemeral and lost on restart
- `Data/` (uppercase) = source code, `AppData/` = runtime storage

See [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md) for deployment and backup strategies.

## Additional Resources

- [README.md](README.md) - Project overview and quick start
- [README.Docker.md](README.Docker.md) - Comprehensive Docker deployment guide
- [CICD-SETUP.md](CICD-SETUP.md) - CI/CD pipeline configuration
- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) - System architecture
- [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md) - Development guide
- [docs/FEATURES.md](docs/FEATURES.md) - Feature documentation
- [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md) - Deployment guide

## Claude Code Workflow

When working with Claude Code on this project:

- **Use skills** - Invoke appropriate skills for tasks (e.g., `git-branch-manager` for git operations, `security-owasp` for security review)
- **Update CLAUDE.md** - Keep this file updated when adding new features or services
- **Git workflow** - Use feature branches (e.g., `feature/feature-name`), never commit directly to main
- **Verify builds** - Run `dotnet build` and `dotnet test` before committing
- **Stop running app first** - If build fails with file lock error, stop the running application before building

### MCP Servers Available

#### Microsoft Learn

Access official Microsoft and Azure documentation for .NET, ASP.NET Core, Blazor, and Entity Framework guidance.

| Tool | Purpose |
|------|---------|
| `microsoft_docs_search` | Search docs, returns up to 10 content chunks (500 tokens each) with title, URL, excerpt |
| `microsoft_code_sample_search` | Search for code snippets, returns up to 20 samples (optional `language` filter) |
| `microsoft_docs_fetch` | Fetch full documentation page as markdown for detailed content |

**Workflow**: Search first for overview → Code samples for examples → Fetch for full details when needed

### Custom Agents

Project-specific agents in `.claude/agents/`:

| Agent | Purpose |
|-------|---------|
| `csharp-function-manager` | Create, edit, and refactor C# functions/services with security patterns, Microsoft docs lookup, and architecture validation |
| `csharp-test-writer` | Write unit and integration tests following xUnit conventions |
| `architecture-enforcer` | Validate file placement and ensure architectural consistency |
| `razor-feature-implementer` | Implement Razor component features following project patterns |
| `razor-ux-reviewer` | Review Razor components for UX and accessibility |
| `git-branch-manager` | Manage git branches and push workflows |

**When to use `csharp-function-manager`**: Creating, editing, or refactoring service methods, API endpoints, or any C# function. It manages the full code lifecycle while applying OWASP security patterns and validating architecture.

## Support

For issues and questions:
- GitHub Issues: https://github.com/helpower2/Madtorio/issues
- Check relevant documentation in [docs/](docs/) directory
