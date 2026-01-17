# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Madtorio is a Blazor Server web application (.NET 10) for managing Factorio game save files. It provides user authentication with admin roles and a file management system for uploading, storing, and downloading save files.

## Build and Run Commands

```bash
# Build the project
dotnet build

# Run in development mode
dotnet run

# Run with hot reload
dotnet watch

# Apply EF Core migrations
dotnet ef database update

# Add a new migration
dotnet ef migrations add <MigrationName> --output-dir Data/Migrations
```

## Architecture

### Technology Stack
- **Framework**: Blazor Server with Interactive Server Components (.NET 10)
- **Database**: SQLite with Entity Framework Core
- **Authentication**: ASP.NET Core Identity with role-based authorization

### Key Directories
- `Components/` - Razor components
  - `Pages/` - Page components (routes)
  - `Pages/Admin/` - Admin-only pages (protected by "AdminOnly" policy)
  - `Layout/` - Layout components (MainLayout, NavMenu)
  - `Shared/` - Reusable components
  - `Account/` - Identity/authentication components
- `Data/` - Database layer
  - `Models/` - Entity models
  - `Seed/` - Database seeding (DbInitializer)
  - `Migrations/` - EF Core migrations
- `Services/` - Business logic services

### Service Layer Pattern
Services follow interface-based dependency injection:
- `ISaveFileService` / `SaveFileService` - CRUD operations for save file metadata
- `IFileStorageService` / `FileStorageService` - Physical file storage operations

### Database
- Uses SQLite database (`madtorio.db`)
- Connection string in `appsettings.json`
- Database auto-migrates and seeds admin user on startup via `DbInitializer`

### Authentication
- Identity configured with role support ("Admin" role)
- Default admin credentials seeded at startup (email: admin@madtorio.com)
- Admin pages protected with `[Authorize(Policy = "Admin")]`
- File upload limit: 500MB configured in `FormOptions`

## Development Guidelines

- When creating new components, add them to the component test page (`/admin/component-test`)
- Only git commit when a feature is complete and tested
