# Madtorio Architecture

This document describes the technical architecture of the Madtorio application.

## Technology Stack

- **Framework**: Blazor Server with Interactive Server Components (.NET 10)
- **Database**: SQLite with Entity Framework Core
- **Authentication**: ASP.NET Core Identity with role-based authorization
- **Container Runtime**: Docker with multi-platform support (linux/amd64, linux/arm64)

## Project Structure

### Key Directories

```
Madtorio/
├── Components/              # Razor components
│   ├── Pages/              # Page components (routes)
│   │   ├── Admin/          # Admin-only pages (protected by "Admin" policy)
│   │   └── Account/        # Identity/authentication components
│   ├── Layout/             # Layout components (MainLayout, NavMenu)
│   └── Shared/             # Reusable components
├── Data/                   # Database layer (source code)
│   ├── Models/             # Entity models
│   ├── Migrations/         # EF Core migrations
│   └── Seed/               # Database seeding (DbInitializer)
├── data/                   # Runtime data storage (git-ignored, created at startup)
│   ├── uploads/saves/      # Uploaded Factorio save files
│   ├── uploads/temp/       # Temporary chunked upload storage
│   └── keys/               # ASP.NET Core Data Protection keys
├── Services/               # Business logic services
├── Middleware/             # Custom middleware components
├── Controllers/            # API controllers
└── Madtorio.Tests/         # Test project
```

**Important Note**: The `Data/` directory (uppercase) in the repository contains **source code** (Models, Migrations, Seed). The `AppData/` directory (lowercase) is created at runtime for **data storage**. On Windows (case-insensitive filesystem), both paths resolve to the same directory name but serve different purposes.

## Service Layer Pattern

All business logic is implemented using interface-based dependency injection, following the repository pattern:

### Core Services

#### ISaveFileService / SaveFileService
- CRUD operations for save file metadata
- Manages SaveFile entity records in the database
- Handles file metadata queries and validation

#### IFileStorageService / FileStorageService
- Physical file storage operations on disk
- Manages actual .zip file storage in `AppData/uploads/saves/`
- Handles file deletion and cleanup

#### IChunkedFileUploadService / ChunkedFileUploadService
- Handles large file uploads via chunking
- Manages temporary chunks in `AppData/uploads/temp/`
- Assembles chunks into final save files
- Supports files up to 500MB

#### IRulesService / RulesService
- Rules and server configuration management
- CRUD operations for RuleCategory and Rule entities
- Server configuration key-value storage
- Provides grouped rules for public display

#### IStatisticsService / StatisticsService
- Tracks application usage statistics
- Records save file events (uploads, downloads, deletions)
- Provides admin dashboard metrics

### Service Registration

Services are registered in [Program.cs](../Program.cs) using the built-in dependency injection container:

```csharp
builder.Services.AddScoped<ISaveFileService, SaveFileService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IChunkedFileUploadService, ChunkedFileUploadService>();
builder.Services.AddScoped<IRulesService, RulesService>();
builder.Services.AddScoped<IStatisticsService, StatisticsService>();
```

## Database Architecture

### Database Engine
- **SQLite** with Write-Ahead Logging (WAL) mode enabled
- Connection string configured in `appsettings.json`
- Auto-migrates on startup via `DbInitializer`

### Core Entities

#### User (Identity)
- Managed by ASP.NET Core Identity
- Supports "Admin" role for administrative access
- Email-based authentication

#### SaveFile
- Metadata for uploaded Factorio save files
- Properties: FileName, FileSize, UploadedAt, UploadedBy
- References User entity

#### RuleCategory
- Organizational categories for rules
- Properties: Name, Description, DisplayOrder, IsEnabled
- Soft delete support via IsEnabled flag
- Has many Rules

#### Rule
- Individual community or server rules
- Properties: Content, DetailedDescription, DisplayOrder, IsEnabled
- Belongs to RuleCategory
- Soft delete support

#### ServerConfig
- Key-value configuration storage
- Properties: Key, Value, Description, ModifiedDate, ModifiedBy
- Unique key constraint

#### Statistics
- Usage tracking and metrics
- Properties: EventType, EntityId, UserId, Timestamp, Details
- Supports various event types (Upload, Download, Delete, etc.)

### Database Initialization

The `DbInitializer` class seeds initial data on first run:
- Default admin user (email: admin@madtorio.com)
- Rule categories and rules (15 total rules)
- Server configuration (Factorio server IP)

See [FEATURES.md](FEATURES.md) for details on the Rules System schema.

## Authentication & Authorization

### Identity Configuration
- ASP.NET Core Identity with role support
- "Admin" role for administrative functions
- Cookie-based authentication
- Data Protection keys stored in `AppData/keys/`

### Authorization Policies
- **"Admin" policy**: Requires authenticated user with "Admin" role
- Applied via `[Authorize(Policy = "Admin")]` attribute on admin pages

### Admin Pages Protection
All pages under `Components/Pages/Admin/` are protected:
- `/admin/users` - User management
- `/admin/rules` - Rules and server configuration
- `/admin/statistics` - Usage statistics dashboard
- `/admin/component-test` - Component testing page

### File Upload Limits
- Maximum file size: 500MB
- Configured in `Program.cs` via `FormOptions`
- Client-side validation enforced
- Server-side validation in upload services

## Component Architecture

### Blazor Server Components
- Interactive Server render mode for real-time updates
- SignalR for client-server communication
- Scoped service lifetime per circuit

### Layout Structure
- **MainLayout.razor**: Primary application layout
- **NavMenu.razor**: Navigation sidebar
- Conditional rendering based on authentication state

### Page Organization
- Public pages: Home, Rules, Server Info
- Protected pages: Admin section
- Account pages: Login, Register, Profile

## Middleware Pipeline

Custom middleware components handle cross-cutting concerns:
- Request logging
- Error handling
- Security headers
- Static file serving

For implementation details, see the [Middleware/](../Middleware/) directory.

## Data Flow

### File Upload Flow
1. User selects file in UI (Components/Pages/SaveFiles)
2. Client chunks large files (JavaScript)
3. ChunkedFileUploadService receives chunks
4. Chunks assembled in `AppData/uploads/temp/`
5. SaveFileService creates metadata record
6. FileStorageService moves final file to `AppData/uploads/saves/`
7. StatisticsService records upload event
8. Temporary chunks cleaned up

### Authentication Flow
1. User submits credentials (Components/Account/Login)
2. Identity verifies credentials against database
3. Authentication cookie created (encrypted with Data Protection keys)
4. User redirected to return URL or home page
5. Subsequent requests validated via cookie

### Rules Display Flow
1. Public page requests rules (Components/Pages/Rules)
2. RulesService queries enabled categories and rules
3. Results grouped by category with display order
4. Server IP fetched from ServerConfig
5. Rendered in categorized sections

## Security Considerations

### Authentication
- Passwords hashed with Identity's default hasher (PBKDF2)
- Data Protection keys secure authentication cookies
- HTTPS recommended for production (via reverse proxy)

### Authorization
- Role-based access control for admin functions
- Policy-based authorization on sensitive endpoints
- File uploads restricted by size and extension

### File Storage
- Files stored outside web root
- Direct file access prevented by framework
- Downloads served through controller with authorization checks

### Docker Security
- Runs as non-root user (UID 1654)
- Minimal base image (mcr.microsoft.com/dotnet/aspnet:10.0)
- Build-time and runtime separation

## Performance Considerations

### Database
- SQLite WAL mode for better concurrency
- Indexed foreign keys for query performance
- Soft deletes avoid expensive hard delete operations

### File Storage
- Chunked uploads reduce memory pressure
- Streaming downloads for large files
- Temporary file cleanup on startup and after uploads

### Blazor Server
- Scoped services per circuit reduce memory usage
- SignalR connection management
- Circuit timeout configured for long-running operations

## Testing Architecture

Tests are organized in the `Madtorio.Tests/` project:
- Unit tests for services
- Integration tests for database operations
- Component tests for Blazor components

See [DEVELOPMENT.md](DEVELOPMENT.md) for testing guidelines.

## Deployment Architecture

See [DEPLOYMENT.md](DEPLOYMENT.md) for details on:
- Docker containerization
- Volume management
- Production configuration
- Unraid deployment
