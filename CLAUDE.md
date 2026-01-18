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
- `Data/` - Database layer (source code)
  - `Models/` - Entity models
  - `Seed/` - Database seeding (DbInitializer)
  - `Migrations/` - EF Core migrations
- `data/` - Runtime data storage (git-ignored, created at startup)
  - `uploads/saves/` - Uploaded Factorio save files
  - `uploads/temp/` - Temporary chunked upload storage
  - `keys/` - ASP.NET Core Data Protection keys
- `Services/` - Business logic services

### Service Layer Pattern
Services follow interface-based dependency injection:
- `ISaveFileService` / `SaveFileService` - CRUD operations for save file metadata
- `IFileStorageService` / `FileStorageService` - Physical file storage operations
- `IChunkedFileUploadService` / `ChunkedFileUploadService` - Chunked file upload handling
- `IRulesService` / `RulesService` - Rules and server configuration management

### Database
- Uses SQLite database (`madtorio.db`)
- Connection string in `appsettings.json`
- Database auto-migrates and seeds data on startup via `DbInitializer`:
  - Admin user (email: admin@madtorio.com)
  - Rule categories and rules (15 total rules)
  - Server configuration (Factorio server IP)

### Authentication
- Identity configured with role support ("Admin" role)
- Default admin credentials seeded at startup (email: admin@madtorio.com)
- Admin pages protected with `[Authorize(Policy = "Admin")]`
- File upload limit: 500MB configured in `FormOptions`

### Data Storage

**Development Environment:**
- Database: `madtorio.db` (project root)
  - Includes WAL files: `madtorio.db-shm`, `madtorio.db-wal`
  - Git-ignored
- Uploaded saves: `data/uploads/saves/`
- Temporary uploads: `data/uploads/temp/`
- Data protection keys: `data/keys/`
- All runtime data directories are git-ignored

**Production/Docker Environment:**
- All data consolidated in: `/app/data/`
- Database: `/app/data/madtorio.db`
- Uploaded saves: `/app/data/uploads/saves/`
- Temporary uploads: `/app/data/uploads/temp/`
- Data protection keys: `/app/data/keys/`

**Docker Volume Mapping:**
- docker-compose.yml: `./data:/app/data` (relative to project directory)
- Recommended for Unraid: `/mnt/user/appdata/madtorio:/app/data`
- **IMPORTANT:** Without a volume mount, all data is ephemeral and will be lost on container restart/update

**Note:** The `Data/` directory (uppercase) in the repository contains **source code** (Models, Migrations, Seed). The `data/` directory (lowercase) is created at runtime for **data storage**. On Windows (case-insensitive filesystem), both paths resolve to the same directory name but serve different purposes.

### Unraid Deployment

**Template Installation:**
1. Add the Madtorio template to Community Applications, or
2. Import `unraid-template.xml` manually through Unraid's Docker page

**Volume Mapping:**
- Host path: `/mnt/user/appdata/madtorio`
- Container path: `/app/data`
- This mapping persists the database, uploaded save files, and data protection keys

**Port Configuration:**
- Default: 8567
- Change via Unraid template if needed

**First-Time Setup:**
1. Deploy the container from the template
2. Access the web UI at `http://[UNRAID-IP]:8567`
3. Log in with default credentials:
   - Email: admin@madtorio.com (or custom AdminEmail from template)
   - Password: Check logs for auto-generated password or set custom AdminPassword in template

**Data Backup:**
- Back up `/mnt/user/appdata/madtorio/` to preserve all data
- Includes database, uploaded saves, and encryption keys

**Troubleshooting:**
- If `/mnt/user/appdata/madtorio/` is empty, check the volume mapping in the container settings
- Run `docker exec madtorio ls -la /app/data` to verify data is being stored correctly inside the container

## Rules System

### Overview
The Rules System is an admin-editable database-driven feature for managing community rules and server configuration. Rules are organized into categories and can be managed through the admin interface.

### Database Schema

#### RuleCategory
- `Id` (int, PK)
- `Name` (string, 100 chars, required)
- `Description` (string, 500 chars, nullable)
- `DisplayOrder` (int) - Controls display order
- `IsEnabled` (bool) - Soft delete flag
- `CreatedDate`, `ModifiedDate` - Audit timestamps
- Navigation: `Rules` collection

#### Rule
- `Id` (int, PK)
- `CategoryId` (int, FK to RuleCategory)
- `Content` (string, 2000 chars, required) - Main rule text
- `DetailedDescription` (string, 5000 chars, nullable) - Extended explanation
- `DisplayOrder` (int) - Controls display order within category
- `IsEnabled` (bool) - Soft delete flag
- `CreatedDate`, `ModifiedDate` - Audit timestamps
- Navigation: `Category`

#### ServerConfig
- `Id` (int, PK)
- `Key` (string, 50 chars, unique, required)
- `Value` (string, 500 chars, required)
- `Description` (string, 200 chars, nullable)
- `ModifiedDate`, `ModifiedBy` - Audit fields

### Default Seeded Data

**General Rules Category** (6 rules):
1. Keep it in english
2. No racism, sexism, bigotry, etc.
3. No NSFW posts; this includes images, links and other content
4. No charged conversations, or being toxic, treat everyone with respect
5. No drama. Leave private matters private, and avoid starting conflict with other members of the server
6. No abusing reactions. This includes spelling hateful things and stirring up drama through them

**Factorio Server Rules Category** (9 rules):
1. All general rules still apply
2. No griefing; which includes killing players out of malice
3. No game breaking bug abuse
4. Keep the arguments civil and respectful, don't get personal
5. Avoid excessive or selfish use of shared resources (with detailed description about resource management)
6. Respect server limits; avoid creating unnecessarily resource-intensive or lag-inducing systems
7. Each planet should be a unified system, since you can only have 1 cargo landing pad
8. Complete pre-made factory blueprints from external sources are not allowed. Using individual blueprints (miners, smelters, etc.) is fine. Build the overall base collaboratively
9. Make an effort to complete your projects that fall under the category of factory critical

**Server IP**: 192.67.197.11

### Key Pages

#### Public Pages
- `/rules` - Displays all active rules grouped by category, plus server IP
- `/server-info` - Dedicated page for server connection information with instructions
- `/` (Home) - Shows server IP in a highlighted card

#### Admin Pages
- `/admin/rules` - Full CRUD interface for managing rules and categories
  - Two tabs: "Rules Management" and "Server Settings"
  - Inline editing for categories and rules
  - Up/down arrows for reordering
  - Enable/disable toggles
  - Delete with confirmation

### RulesService API

**Category Operations:**
- `GetAllCategoriesAsync(includeDisabled)` - Get all categories with rules
- `GetCategoryByIdAsync(id)` - Get specific category
- `CreateCategoryAsync(category)` - Create new category
- `UpdateCategoryAsync(category)` - Update existing category
- `DeleteCategoryAsync(id)` - Delete category (cascade deletes rules)
- `ReorderCategoriesAsync(categoryIds)` - Update display order

**Rule Operations:**
- `GetRulesByCategoryAsync(categoryId, includeDisabled)` - Get rules for category
- `GetRuleByIdAsync(id)` - Get specific rule
- `CreateRuleAsync(rule)` - Create new rule
- `UpdateRuleAsync(rule)` - Update existing rule
- `DeleteRuleAsync(id)` - Delete rule
- `ReorderRulesAsync(categoryId, ruleIds)` - Update display order within category

**Display Operations:**
- `GetActiveRulesGroupedAsync()` - Get dictionary of active categories with their active rules (for public display)

**Server Config Operations:**
- `GetServerConfigAsync(key)` - Get config value by key
- `SetServerConfigAsync(key, value, modifiedBy)` - Set config value

### Implementation Notes
- Uses soft deletes (IsEnabled flag) to preserve history
- DisplayOrder allows admin-controlled sorting without complex SQL
- Detailed descriptions support extended explanations (e.g., rule #5 resource management)
- All admin operations require "Admin" role authorization
- Public pages only show enabled categories and rules
- Server IP cached in memory for performance

## Development Guidelines

- When implementing a feature, switch to or create a dedicated git branch for that feature
- Implement tests for all new features and functionality
- Update this CLAUDE.md file when making architectural changes, adding new services, or implementing significant features
- When creating new components, add them to the component test page (`/admin/component-test`)
- Only git commit when a feature is complete and tested
- Rules system follows the same service pattern as SaveFiles - interface-based DI with comprehensive CRUD operations
