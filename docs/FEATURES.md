# Madtorio Features

This document provides detailed documentation for major features in the Madtorio application.

## Table of Contents

- [Rules System](#rules-system)
- [Save File Management](#save-file-management)
- [Statistics Tracking](#statistics-tracking)

---

## Rules System

### Overview

The Rules System is an admin-editable database-driven feature for managing community rules and server configuration. Rules are organized into categories and can be reordered, enabled/disabled, and managed through a comprehensive admin interface.

### Database Schema

#### RuleCategory

Represents a category or grouping of rules.

**Properties**:
- `Id` (int, PK) - Unique identifier
- `Name` (string, 100 chars, required) - Category name (e.g., "General Rules")
- `Description` (string, 500 chars, nullable) - Optional category description
- `DisplayOrder` (int) - Controls display order (lower numbers appear first)
- `IsEnabled` (bool) - Soft delete flag (false = hidden, not deleted)
- `CreatedDate` (DateTime) - Audit timestamp
- `ModifiedDate` (DateTime) - Audit timestamp
- **Navigation**: `Rules` - Collection of rules in this category

**Indexes**:
- Primary key on `Id`
- Non-unique index on `DisplayOrder` for sorting

#### Rule

Represents an individual rule within a category.

**Properties**:
- `Id` (int, PK) - Unique identifier
- `CategoryId` (int, FK) - Foreign key to RuleCategory
- `Content` (string, 2000 chars, required) - Main rule text
- `DetailedDescription` (string, 5000 chars, nullable) - Extended explanation or examples
- `DisplayOrder` (int) - Controls display order within category
- `IsEnabled` (bool) - Soft delete flag
- `CreatedDate` (DateTime) - Audit timestamp
- `ModifiedDate` (DateTime) - Audit timestamp
- **Navigation**: `Category` - Parent RuleCategory

**Indexes**:
- Primary key on `Id`
- Foreign key index on `CategoryId`
- Non-unique index on `DisplayOrder` for sorting

#### ServerConfig

Key-value configuration storage for server settings.

**Properties**:
- `Id` (int, PK) - Unique identifier
- `Key` (string, 50 chars, unique, required) - Configuration key (e.g., "ServerIP")
- `Value` (string, 500 chars, required) - Configuration value
- `Description` (string, 200 chars, nullable) - Human-readable description
- `ModifiedDate` (DateTime) - Last modification timestamp
- `ModifiedBy` (string, nullable) - User who last modified

**Indexes**:
- Primary key on `Id`
- Unique index on `Key`

### Default Seeded Data

The database is seeded with default rules on first run via `DbInitializer`.

#### General Rules Category

Contains 6 community conduct rules:

1. Keep it in english
2. No racism, sexism, bigotry, etc.
3. No NSFW posts; this includes images, links and other content
4. No charged conversations, or being toxic, treat everyone with respect
5. No drama. Leave private matters private, and avoid starting conflict with other members of the server
6. No abusing reactions. This includes spelling hateful things and stirring up drama through them

#### Factorio Server Rules Category

Contains 9 game-specific rules:

1. All general rules still apply
2. No griefing; which includes killing players out of malice
3. No game breaking bug abuse
4. Keep the arguments civil and respectful, don't get personal
5. Avoid excessive or selfish use of shared resources
   - **Detailed Description**: "Before claiming large amounts of shared resources (ore patches, oil fields, etc.), coordinate with other players. Don't monopolize resources that others need. If you're building a megabase, plan resource usage thoughtfully and communicate your needs to the community."
6. Respect server limits; avoid creating unnecessarily resource-intensive or lag-inducing systems
7. Each planet should be a unified system, since you can only have 1 cargo landing pad
8. Complete pre-made factory blueprints from external sources are not allowed. Using individual blueprints (miners, smelters, etc.) is fine. Build the overall base collaboratively
9. Make an effort to complete your projects that fall under the category of factory critical

#### Server Configuration

- **ServerIP**: `192.67.197.11`

### User Pages

#### Public Pages

##### /rules

Displays all active rules grouped by category, plus server connection information.

**Features**:
- Shows only enabled categories and rules
- Grouped by category with category descriptions
- Rules displayed in display order
- Server IP shown prominently
- Mobile-responsive design

**Access**: Public (no authentication required)

##### /server-info

Dedicated page for server connection information with detailed instructions.

**Features**:
- Server IP with copy-to-clipboard button
- Connection instructions
- Client setup guide

**Access**: Public

##### / (Home)

Shows server IP in a highlighted card on the home page.

**Features**:
- Quick access to server IP
- Link to detailed server info page

**Access**: Public

#### Admin Pages

##### /admin/rules

Full CRUD interface for managing rules, categories, and server configuration.

**Features**:
- **Two-tab interface**:
  1. **Rules Management**: Manage categories and rules
  2. **Server Settings**: Manage server configuration

**Rules Management Tab**:
- View all categories with their rules
- Inline editing for categories (name, description)
- Inline editing for rules (content, detailed description)
- Reordering via up/down arrows
- Enable/disable toggles for soft deletes
- Add new categories and rules
- Delete with confirmation dialog
- Real-time updates

**Server Settings Tab**:
- Edit server IP
- Edit other configuration key-value pairs
- Description field for each setting
- Audit trail (modified date, modified by)

**Authorization**: Requires "Admin" role

### IRulesService API

The `IRulesService` interface provides comprehensive operations for rules management.

#### Category Operations

```csharp
// Get all categories with their rules
Task<List<RuleCategory>> GetAllCategoriesAsync(bool includeDisabled = false);

// Get specific category by ID
Task<RuleCategory?> GetCategoryByIdAsync(int id);

// Create new category
Task<RuleCategory> CreateCategoryAsync(RuleCategory category);

// Update existing category
Task UpdateCategoryAsync(RuleCategory category);

// Delete category (cascade deletes all rules in category)
Task DeleteCategoryAsync(int id);

// Reorder categories by providing new order of IDs
Task ReorderCategoriesAsync(List<int> categoryIds);
```

#### Rule Operations

```csharp
// Get rules for specific category
Task<List<Rule>> GetRulesByCategoryAsync(int categoryId, bool includeDisabled = false);

// Get specific rule by ID
Task<Rule?> GetRuleByIdAsync(int id);

// Create new rule
Task<Rule> CreateRuleAsync(Rule rule);

// Update existing rule
Task UpdateRuleAsync(Rule rule);

// Delete rule
Task DeleteRuleAsync(int id);

// Reorder rules within a category
Task ReorderRulesAsync(int categoryId, List<int> ruleIds);
```

#### Display Operations

```csharp
// Get dictionary of active categories with their active rules (for public display)
Task<Dictionary<RuleCategory, List<Rule>>> GetActiveRulesGroupedAsync();
```

#### Server Config Operations

```csharp
// Get configuration value by key
Task<string?> GetServerConfigAsync(string key);

// Set configuration value (creates if not exists, updates if exists)
Task SetServerConfigAsync(string key, string value, string? modifiedBy = null);
```

### Implementation Notes

- **Soft Deletes**: Uses `IsEnabled` flag instead of hard deletes to preserve history
- **Display Order**: Admin-controlled ordering without complex SQL queries
- **Detailed Descriptions**: Support for extended explanations (e.g., rule #5 resource management guidelines)
- **Authorization**: All admin operations require "Admin" role
- **Public Display**: Only enabled categories and rules are shown to public
- **Caching**: Server IP can be cached in memory for performance (optional)
- **Audit Trail**: CreatedDate and ModifiedDate tracked on all entities
- **Cascading Deletes**: Deleting a category deletes all its rules

### Usage Examples

#### Display Rules on Public Page

```csharp
@inject IRulesService RulesService

var rulesGrouped = await RulesService.GetActiveRulesGroupedAsync();
foreach (var (category, rules) in rulesGrouped)
{
    <h2>@category.Name</h2>
    @if (!string.IsNullOrEmpty(category.Description))
    {
        <p>@category.Description</p>
    }
    <ol>
        @foreach (var rule in rules)
        {
            <li>
                @rule.Content
                @if (!string.IsNullOrEmpty(rule.DetailedDescription))
                {
                    <p class="detail">@rule.DetailedDescription</p>
                }
            </li>
        }
    </ol>
}
```

#### Reorder Rules in Admin Panel

```csharp
@inject IRulesService RulesService

async Task MoveRuleUp(int ruleId, List<Rule> rules)
{
    var index = rules.FindIndex(r => r.Id == ruleId);
    if (index > 0)
    {
        var ruleIds = rules.Select(r => r.Id).ToList();
        ruleIds.RemoveAt(index);
        ruleIds.Insert(index - 1, ruleId);
        await RulesService.ReorderRulesAsync(rules[0].CategoryId, ruleIds);
    }
}
```

---

## Save File Management

### Overview

The Save File Management system allows users to upload, store, and download Factorio save files. It supports large files (up to 500MB) through chunked uploads.

### Features

- **Chunked Uploads**: Large files split into chunks for reliable uploads
- **File Metadata**: Track filename, size, upload date, and uploader
- **Authorization**: Only authenticated users can upload
- **File Storage**: Files stored in `data/uploads/saves/`
- **Temporary Storage**: Chunks stored in `data/uploads/temp/` during upload
- **Download Tracking**: Statistics recorded for each download

### Components

#### ISaveFileService / SaveFileService

Manages save file metadata in the database.

**Key Methods**:
- `GetAllSaveFilesAsync()` - List all save files
- `GetSaveFileByIdAsync(id)` - Get specific save file metadata
- `CreateSaveFileAsync(saveFile)` - Create metadata record
- `DeleteSaveFileAsync(id)` - Delete metadata record

#### IFileStorageService / FileStorageService

Manages physical file storage on disk.

**Key Methods**:
- `SaveFileAsync(stream, filename)` - Save file to disk
- `GetFileStreamAsync(filename)` - Retrieve file for download
- `DeleteFileAsync(filename)` - Delete file from disk
- `GetFileSizeAsync(filename)` - Get file size

#### IChunkedFileUploadService / ChunkedFileUploadService

Handles chunked file uploads for large files.

**Key Methods**:
- `UploadChunkAsync(chunkData, chunkIndex, totalChunks, uploadId)` - Upload single chunk
- `AssembleChunksAsync(uploadId, filename)` - Assemble chunks into final file
- `CleanupChunksAsync(uploadId)` - Remove temporary chunks

### Upload Flow

1. Client splits file into chunks (JavaScript)
2. Each chunk uploaded via `ChunkedFileUploadService`
3. Chunks stored in `data/uploads/temp/`
4. Final chunk triggers assembly
5. Complete file moved to `data/uploads/saves/`
6. Metadata created via `SaveFileService`
7. Statistics event recorded
8. Temporary chunks cleaned up

---

## Statistics Tracking

### Overview

The Statistics system tracks usage metrics for administrative dashboards and analytics.

### Tracked Events

- **Upload**: Save file uploaded
- **Download**: Save file downloaded
- **Delete**: Save file deleted
- **Login**: User login
- **Registration**: New user registration
- **PageView**: Page views (optional)

### Database Schema

#### Statistics Entity

**Properties**:
- `Id` (int, PK) - Unique identifier
- `EventType` (string, required) - Type of event (Upload, Download, etc.)
- `EntityId` (string, nullable) - Related entity ID (e.g., save file ID)
- `UserId` (string, nullable) - User who triggered event
- `Timestamp` (DateTime) - When event occurred
- `Details` (string, nullable) - Additional JSON details

### IStatisticsService API

```csharp
// Record a statistics event
Task RecordEventAsync(string eventType, string? entityId = null, string? userId = null, string? details = null);

// Get event counts for dashboard
Task<Dictionary<string, int>> GetEventCountsAsync(DateTime? since = null);

// Get recent events
Task<List<Statistics>> GetRecentEventsAsync(int count = 100);

// Get events by type
Task<List<Statistics>> GetEventsByTypeAsync(string eventType, int count = 100);
```

### Admin Dashboard

Located at `/admin/statistics`, shows:
- Total uploads, downloads, deletes
- Active users count
- Recent activity timeline
- Event type breakdown
- Date range filtering

**Authorization**: Requires "Admin" role

### Usage Example

```csharp
@inject IStatisticsService StatisticsService

// Record upload event
await StatisticsService.RecordEventAsync(
    "Upload",
    saveFile.Id.ToString(),
    currentUserId,
    $"File: {saveFile.FileName}, Size: {saveFile.FileSize}"
);

// Get dashboard data
var eventCounts = await StatisticsService.GetEventCountsAsync(DateTime.UtcNow.AddDays(-30));
var uploadCount = eventCounts.GetValueOrDefault("Upload", 0);
```

---

## Future Features

Potential features for future development:

- **File Versioning**: Track multiple versions of same save file
- **Tags and Categories**: Organize saves by tags
- **Sharing**: Share save files with specific users or groups
- **Comments**: Allow users to comment on save files
- **Thumbnails**: Generate and display save file thumbnails
- **Advanced Search**: Search by filename, uploader, date range
- **Bulk Operations**: Delete or download multiple files
- **Notifications**: Notify users of new uploads or updates
