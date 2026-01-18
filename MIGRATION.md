# Migration Guide: Runtime Data Storage Move (data/ → AppData/)

## Overview

This guide helps you migrate from the old `data/` (lowercase) runtime storage directory to the new `AppData/` directory structure.

**Why this change?**
- On Windows, `Data/` (source code) and `data/` (runtime) resolved to the same directory due to case-insensitivity
- This caused confusion between source code and runtime data
- `AppData/` provides clear separation: source code stays in `Data/`, runtime data goes to `AppData/`

**Important:** All data remains valid after migration. The directory is simply renamed. No database migration needed.

---

## For Development Environments

### Windows (PowerShell)

```powershell
# Navigate to your Madtorio project directory
cd C:\Path\To\Madtorio

# Stop any running instance of the application
# (Ctrl+C if running in terminal, or stop the IDE)

# Rename the directory
Move-Item -Path "data" -Destination "AppData"

# Verify the migration
if (Test-Path "AppData") {
    Write-Host "✓ Migration successful: data/ renamed to AppData/"
} else {
    Write-Host "✗ Migration failed"
}

# Start the application
dotnet run
# The application will use AppData/ automatically
```

### Linux / macOS

```bash
# Navigate to your Madtorio project directory
cd ~/path/to/Madtorio

# Stop any running instance
# (Ctrl+C if running in terminal)

# Rename the directory
mv data AppData

# Verify the migration
if [ -d "AppData" ]; then
    echo "✓ Migration successful: data/ renamed to AppData/"
else
    echo "✗ Migration failed"
fi

# Start the application
dotnet run
# The application will use AppData/ automatically
```

### Verification After Migration

```bash
# Build the project
dotnet build

# Run tests to verify file storage works
dotnet test

# Run the application locally
dotnet run

# Check that files are created in the new location:
# - AppData/madtorio.db (database)
# - AppData/keys/ (Data Protection keys)
# - AppData/uploads/saves/ (uploaded save files - if any uploads made)
# - AppData/uploads/temp/ (temporary chunks during upload)
```

---

## For Docker Deployments

### Quick Migration (docker-compose)

```bash
# Stop the container
docker compose down

# Navigate to the directory with docker-compose.yml
cd /path/to/madtorio

# Rename the volume directory
mv data AppData

# Update docker-compose.yml:
# Change line 16 from:
#   - ./data:/app/data
# To:
#   - ./AppData:/app/AppData

# Start the container
docker compose up -d

# Verify it's running
docker compose logs -f

# Check the container has access to the database
docker exec madtorio ls -la /app/AppData/
```

### Manual Docker Compose File Update

Before running `docker compose up`, ensure your `docker-compose.yml` has these changes:

**Line 14** - Connection string environment variable:
```yaml
- ConnectionStrings__DefaultConnection=DataSource=/app/AppData/madtorio.db;Cache=Shared
```

**Line 16** - Volume mount:
```yaml
- ./AppData:/app/AppData
```

### Using Named Volume Instead of Bind Mount

If you prefer to use Docker named volumes instead of a bind mount:

```yaml
services:
  madtorio:
    # ... other config ...
    volumes:
      - madtorio-appdata:/app/AppData

volumes:
  madtorio-appdata:
```

### Migrating Existing Named Volume

If you already have a named volume from the old setup:

```bash
# Create new volume and copy data
docker volume create madtorio-appdata-new

# Copy data from old volume to new
docker run --rm -v madtorio-data:/old -v madtorio-appdata-new:/new \
  alpine cp -r /old/. /new/

# Update docker-compose.yml to use madtorio-appdata-new
# Remove old volume if no longer needed
docker volume rm madtorio-data
```

---

## For Unraid Installations

### Step-by-Step Migration

1. **Stop the Container**
   - Navigate to Docker in the Unraid interface
   - Find the "madtorio" container
   - Click "Force Stop"

2. **Update Volume Mount**
   - Go to the container settings
   - Edit the container
   - Find the volume configuration for `/app/data`
   - **Change the container mount path from** `/app/data` **to** `/app/AppData`
   - Keep the host path as `/mnt/user/appdata/madtorio` (or your configured path)

3. **Start the Container**
   - Save changes
   - Click "Start"
   - Monitor logs to ensure startup is clean

4. **Verify**
   - Open web browser to `http://<unraid-ip>:8567`
   - Log in with admin credentials
   - Try uploading a file to verify the storage is working
   - Check existing save files are still accessible

### Updating the Unraid Template

If you're using the Madtorio Unraid template, update it:

- Find the Unraid template XML configuration for Madtorio
- Update the path mapping from `/app/data` to `/app/AppData`
- Save and redeploy from the template

The updated path in the template should be:
```xml
<Config Name="Data Directory" Target="/app/AppData" Default="/mnt/user/appdata/madtorio" ...
```

---

## For Existing Backups

### Backup Migration

If you have backups made with the old `data/` directory structure, no special migration is needed. The database file can be restored directly:

```bash
# Old backup format: backup/madtorio.db (from data/ directory)
# Restore it to: AppData/madtorio.db

# For a full backup:
cp -r backup/data/* AppData/
# This preserves all subdirectories: uploads/, keys/, madtorio.db
```

### Important Paths to Know

**Old structure (before migration):**
```
data/
├── madtorio.db
├── uploads/
│   ├── saves/
│   └── temp/
└── keys/
```

**New structure (after migration):**
```
AppData/
├── madtorio.db
├── uploads/
│   ├── saves/
│   └── temp/
└── keys/
```

The internal structure is identical; only the root directory name changes.

---

## Troubleshooting

### "Directory not found" errors after migration

**Problem:** Application fails to start with "AppData directory not found"

**Solution:**
```bash
# Verify the directory was renamed correctly
ls -la | grep -i appdata

# If it still shows 'data' instead of 'AppData':
mv data AppData

# If 'AppData' doesn't exist at all:
mkdir -p AppData/{uploads/{saves,temp},keys}
# Copy your database if you have a backup:
cp /path/to/backup/madtorio.db AppData/
```

### Database file not found in Docker container

**Problem:** Docker container logs show "madtorio.db not found"

**Solution:**
1. Stop container: `docker compose down`
2. Verify host directory exists: `ls AppData/`
3. Check docker-compose.yml volume mount is correct:
   ```yaml
   - ./AppData:/app/AppData  # Should be AppData on both sides
   ```
4. Start container: `docker compose up -d`

### Upload fails with "Permission denied"

**Problem:** File uploads fail with permission errors

**Solution:**
```bash
# Ensure AppData directory is writable
chmod -R 755 AppData/

# For Docker, ensure permissions on host match container user (usually www-data)
# On Unraid, the path should be accessible to the container user
```

### Old database file not recognized

**Problem:** Application creates new database instead of using existing one

**Solution:**
1. Verify database file exists:
   ```bash
   ls -la AppData/madtorio.db
   ```

2. Verify application's connection string points to AppData:
   ```bash
   # Check appsettings.json
   cat appsettings.json | grep DataSource
   # Should show: "DataSource=AppData/madtorio.db;Cache=Shared"
   ```

3. If database is corrupted, check WAL files:
   ```bash
   ls -la AppData/madtorio.db*
   # Should have: madtorio.db, madtorio.db-shm, madtorio.db-wal
   ```

---

## Rollback (if needed)

If you need to revert to the old structure:

```bash
# Stop the application
dotnet run  # Stop with Ctrl+C

# Rename back
mv AppData data

# Update configuration files back:
# - appsettings.json: "DataSource=data/madtorio.db;Cache=Shared"
# - Program.cs: Path.Combine(..., "data", "keys")
# - And all other path references

# Restart application
dotnet run
```

---

## Verification Checklist

After migration, verify:

- [ ] Application starts without errors
- [ ] Database is accessible (can log in)
- [ ] Existing save files are still accessible
- [ ] Can upload new files without errors
- [ ] Docker container (if applicable) starts and is healthy
- [ ] Backups still work
- [ ] Admin dashboard loads correctly
- [ ] Rules page displays properly

---

## Support

If you encounter migration issues:

1. **Check logs for error messages:**
   ```bash
   # Development
   dotnet run  # Look for startup errors

   # Docker
   docker compose logs -f madtorio
   ```

2. **Verify file permissions:**
   ```bash
   ls -la AppData/
   # Should show rwx permissions for the application user
   ```

3. **Report issues:**
   - GitHub Issues: https://github.com/helpower2/Madtorio/issues
   - Include:
     - OS/platform (Windows, Linux, Docker, Unraid)
     - Full error message
     - Steps you took
     - Output of verification commands

---

## FAQ

**Q: Will I lose my data after migration?**
A: No. Migration is a simple directory rename. All data remains intact. The database file and uploaded files are simply in the new `AppData/` location.

**Q: Do I need to update my database?**
A: No database migration is needed. The application uses the same SQLite database file, just in a new directory.

**Q: Will old backups still work?**
A: Yes. Restore backups to the new `AppData/` directory path instead of the old `data/` path.

**Q: What if I have both `data/` and `AppData/` directories?**
A: The application will use `AppData/`. If you want to use data from the old `data/` directory, rename or copy it to `AppData/` before starting.

**Q: Can I keep using the old `data/` directory?**
A: No. The application now expects `AppData/` exclusively. You must migrate.

**Q: Is this a breaking change?**
A: Yes. Existing deployments must follow this migration guide to update to the new version.

