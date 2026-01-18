# Madtorio Development Guide

This document provides guidance for developers working on the Madtorio codebase.

## Prerequisites

- .NET 10 SDK
- Git
- Docker (optional, for containerized development)
- Visual Studio 2022, VS Code, or Rider (recommended IDEs)

## Getting Started

### Clone Repository

```bash
git clone https://github.com/helpower2/Madtorio.git
cd Madtorio
```

### Restore Dependencies

```bash
dotnet restore
```

### Build Project

```bash
dotnet build
```

### Run Application

```bash
# Run in development mode
dotnet run

# Run with hot reload (recommended for development)
dotnet watch
```

Access the application at `http://localhost:5000`

### Default Admin Credentials

On first run, the database is seeded with a default admin account:
- **Email**: admin@madtorio.com
- **Password**: Check console output for auto-generated password, or set via environment variables

## Build Commands

### Development Build

```bash
dotnet build
```

### Release Build

```bash
dotnet build --configuration Release
```

### Clean Build Artifacts

```bash
dotnet clean
```

## Running the Application

### Development Mode

```bash
dotnet run
```

### With Hot Reload

```bash
dotnet watch
```

Hot reload automatically restarts the application when code changes are detected.

### With Specific Environment

```bash
# Development
ASPNETCORE_ENVIRONMENT=Development dotnet run

# Production
ASPNETCORE_ENVIRONMENT=Production dotnet run
```

## Database Management

### Entity Framework Core Migrations

#### Apply Migrations

Run all pending migrations:

```bash
dotnet ef database update
```

#### Create New Migration

```bash
dotnet ef migrations add <MigrationName> --output-dir Data/Migrations
```

Example:

```bash
dotnet ef migrations add AddUserProfilePicture --output-dir Data/Migrations
```

#### Revert Last Migration

```bash
dotnet ef migrations remove
```

#### View Migration SQL

```bash
dotnet ef migrations script
```

#### Reset Database

```bash
# Delete database file
rm AppData/madtorio.db AppData/madtorio.db-shm AppData/madtorio.db-wal

# Run application (auto-migrates and seeds)
dotnet run
```

### Database Seeding

The database is automatically seeded on first run by `DbInitializer`:
- Admin user
- Rule categories (General Rules, Factorio Server Rules)
- 15 default rules
- Server IP configuration

To modify seed data, edit [Data/Seed/DbInitializer.cs](../Data/Seed/DbInitializer.cs).

## Testing

### Run All Tests

```bash
dotnet test
```

### Run Tests with Verbosity

```bash
dotnet test --verbosity detailed
```

### Run Tests with Coverage

```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Run Specific Test

```bash
dotnet test --filter "FullyQualifiedName~SaveFileServiceTests"
```

### Test Organization

Tests are located in `Madtorio.Tests/`:
- Service tests: `Services/`
- Integration tests: `Integration/`
- Component tests: `Components/`

## Development Guidelines

### Git Workflow

1. **Create Feature Branch**: Always branch from `main`
   ```bash
   git checkout main
   git pull
   git checkout -b feature/your-feature-name
   ```

2. **Make Changes**: Implement your feature with appropriate tests

3. **Commit Regularly**: Use conventional commit messages
   ```bash
   git add .
   git commit -m "feat: Add user profile picture support"
   ```

4. **Push to Remote**:
   ```bash
   git push origin feature/your-feature-name
   ```

5. **Create Pull Request**: Target `main` branch

6. **Wait for CI**: CI must pass before merging

7. **Merge**: After approval and passing CI

### Branch Naming Convention

- `feature/*` - New features
- `fix/*` - Bug fixes
- `hotfix/*` - Critical production fixes
- `refactor/*` - Code refactoring
- `docs/*` - Documentation updates

### Commit Message Convention

Follow [Conventional Commits](https://www.conventionalcommits.org/):

- `feat:` - New feature
- `fix:` - Bug fix
- `docs:` - Documentation changes
- `refactor:` - Code refactoring
- `test:` - Test additions or modifications
- `chore:` - Build process or tooling changes

Examples:
```bash
feat: Add chunked file upload support
fix: Resolve database lock on concurrent uploads
docs: Update architecture documentation
refactor: Extract file storage logic into service
test: Add unit tests for RulesService
```

### Code Quality

#### Before Committing

1. **Build succeeds**: `dotnet build`
2. **Tests pass**: `dotnet test`
3. **No warnings**: Address all compiler warnings
4. **Code formatting**: Follow C# conventions

#### Testing Requirements

- All new features must include tests
- Aim for >80% code coverage on new code
- Test both success and error paths
- Include integration tests for database operations

### Component Development

When creating new Blazor components:

1. **Add to appropriate directory**:
   - Public pages → `Components/Pages/`
   - Admin pages → `Components/Pages/Admin/`
   - Shared components → `Components/Shared/`
   - Layouts → `Components/Layout/`

2. **Add to component test page**: Register in `/admin/component-test` for visual testing

3. **Follow naming conventions**:
   - Page components: `PageName.razor`
   - Shared components: `ComponentName.razor`
   - Code-behind: `ComponentName.razor.cs`

### Service Development

When creating new services:

1. **Define interface**: Create `IServiceName.cs`
2. **Implement service**: Create `ServiceName.cs`
3. **Register in Program.cs**: Add to DI container
4. **Write tests**: Create `ServiceNameTests.cs` in test project

Example:

```csharp
// IMyService.cs
public interface IMyService
{
    Task<Result> DoSomethingAsync();
}

// MyService.cs
public class MyService : IMyService
{
    public async Task<Result> DoSomethingAsync()
    {
        // Implementation
    }
}

// Program.cs
builder.Services.AddScoped<IMyService, MyService>();
```

### Documentation

Update documentation when making significant changes:

- **Architecture changes** → Update [ARCHITECTURE.md](ARCHITECTURE.md)
- **New features** → Update [FEATURES.md](FEATURES.md)
- **Deployment changes** → Update [DEPLOYMENT.md](DEPLOYMENT.md)
- **API changes** → Update relevant service documentation
- **Build/dev changes** → Update this file

## Environment Configuration

### appsettings.json Files

Configuration is split across multiple files:

- `appsettings.json` - Base configuration
- `appsettings.Development.json` - Development overrides
- `appsettings.Production.json` - Production overrides

### Environment Variables

Set via `.env` file or system environment:

- `ASPNETCORE_ENVIRONMENT` - Application environment (Development/Production)
- `AdminEmail` - Default admin email
- `AdminPassword` - Default admin password
- `ConnectionStrings__DefaultConnection` - Database connection string

### Configuration Priority

1. Environment variables (highest)
2. `appsettings.{Environment}.json`
3. `appsettings.json` (lowest)

## Debugging

### Visual Studio / Rider

1. Set breakpoints in code
2. Press F5 to start debugging
3. Application runs with debugger attached

### VS Code

1. Install C# extension
2. Open folder in VS Code
3. Press F5 to launch debugger
4. Select ".NET Core Launch (web)" configuration

### Console Logging

Logging is configured in `appsettings.json`:

```json
"Logging": {
  "LogLevel": {
    "Default": "Information",
    "Microsoft.AspNetCore": "Warning"
  }
}
```

Increase verbosity for debugging:

```json
"Logging": {
  "LogLevel": {
    "Default": "Debug"
  }
}
```

## Common Tasks

### Add New Entity

1. Create model in `Data/Models/`
2. Add DbSet to `ApplicationDbContext`
3. Create migration: `dotnet ef migrations add AddEntity`
4. Apply migration: `dotnet ef database update`
5. Create service interface and implementation
6. Register service in `Program.cs`
7. Add tests

### Add New Admin Page

1. Create page in `Components/Pages/Admin/`
2. Add `@attribute [Authorize(Policy = "Admin")]`
3. Add route: `@page "/admin/pagename"`
4. Add navigation link in `NavMenu.razor` (admin section)
5. Test authorization

### Update Rules or Configuration

1. Edit seed data in `Data/Seed/DbInitializer.cs`
2. For existing databases, create data migration or manual update
3. Test on fresh database

## Troubleshooting

### Database Locked

If you see "database is locked" errors:

```bash
# Stop all running instances
# Delete WAL files
rm AppData/madtorio.db-shm AppData/madtorio.db-wal
# Restart application
```

### Port Already in Use

Change port in `Properties/launchSettings.json` or:

```bash
dotnet run --urls "http://localhost:5001"
```

### Migration Errors

```bash
# Remove last migration
dotnet ef migrations remove

# Reset database
rm AppData/madtorio.db*
dotnet ef database update
```

## Resources

- [Blazor Documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [ASP.NET Core Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity)
- [xUnit Testing](https://xunit.net/)

## Getting Help

- Check existing documentation in `docs/`
- Review [ARCHITECTURE.md](ARCHITECTURE.md) for system design
- See [FEATURES.md](FEATURES.md) for feature-specific details
- Create GitHub issue for bugs or questions
