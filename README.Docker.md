# Madtorio Docker Deployment Guide

This guide covers deploying Madtorio as a Docker container, with specific instructions for Unraid servers.

## Table of Contents
- [Quick Start](#quick-start)
- [Configuration](#configuration)
- [Unraid Installation](#unraid-installation)
- [Volume Management](#volume-management)
- [Security](#security)
- [Troubleshooting](#troubleshooting)
- [Updating](#updating)
- [Data Migration](#data-migration)

## Quick Start

### Prerequisites
- Docker and Docker Compose installed
- .NET 10 SDK (for building, included in Dockerfile)
- Port 8083 available on host machine

### Build and Run

1. Clone the repository:
   ```bash
   cd Madtorio
   ```

2. Create environment file from template:
   ```bash
   cp .env.template .env
   ```

3. Edit `.env` file with your custom admin credentials:
   ```bash
   ADMIN_EMAIL=youremail@example.com
   ADMIN_PASSWORD=YourSecurePassword123!
   ```

4. Build and start the container:
   ```bash
   docker compose up -d
   ```

5. Access the application at `http://localhost:8083`

6. Log in with your configured admin credentials

## Configuration

### Environment Variables

Configure the following environment variables in `.env` file or docker-compose.yml:

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `ADMIN_EMAIL` | Initial admin user email | `admin@madtorio.com` | Yes |
| `ADMIN_PASSWORD` | Initial admin password | `Madtorio2026!` | Yes |
| `ASPNETCORE_ENVIRONMENT` | ASP.NET Core environment | `Production` | No |
| `ASPNETCORE_HTTP_PORTS` | Internal container port | `8080` | No |

### Password Requirements

Admin password must meet these requirements:
- Minimum 6 characters (8+ recommended)
- At least one uppercase letter
- At least one lowercase letter
- At least one digit
- At least one special character

### Connection String

The SQLite database connection string is configured in [appsettings.Production.json](appsettings.Production.json):
```json
"ConnectionStrings": {
  "DefaultConnection": "DataSource=/app/AppData/madtorio.db;Cache=Shared"
}
```

This can be overridden via environment variable:
```bash
ConnectionStrings__DefaultConnection=DataSource=/custom/path/madtorio.db;Cache=Shared
```

## Unraid Installation

### Method 1: Docker Compose (Recommended)

1. **Enable Docker Compose** on Unraid (requires Unraid 6.11+)

2. **Create app directory**:
   ```bash
   mkdir -p /mnt/user/appAppData/madtorio
   cd /mnt/user/appAppData/madtorio
   ```

3. **Copy files** to Unraid:
   - Copy entire Madtorio project directory to `/mnt/user/appAppData/madtorio/`
   - Or use git: `git clone <your-repo-url> .`

4. **Configure environment**:
   ```bash
   cp .env.template .env
   nano .env  # Edit with your credentials
   ```

5. **Start container**:
   ```bash
   docker compose up -d
   ```

6. **Access WebUI**: `http://UNRAID_IP:8083`

### Method 2: Unraid Docker Template

1. Navigate to **Docker** tab in Unraid
2. Click **Add Container**
3. Configure the following settings:

**Basic Settings:**
- **Name**: `madtorio`
- **Repository**: `madtorio:latest` (after building locally)
- **Network Type**: `Bridge`
- **Console shell command**: `Bash`

**Port Mappings:**
| Container Port | Host Port | Type |
|----------------|-----------|------|
| 8080 | 8083 | TCP |

**Volume Mappings:**
| Container Path | Host Path | Mode |
|----------------|-----------|------|
| `/ap./AppData` | `/mnt/user/appAppData/madtori./AppData` | Read/Write |

**Environment Variables:**
| Variable | Value |
|----------|-------|
| `ADMIN_EMAIL` | `youremail@example.com` |
| `ADMIN_PASSWORD` | `YourSecurePassword123!` |
| `ASPNETCORE_ENVIRONMENT` | `Production` |

**Advanced Settings:**
- **Privileged**: `No`
- **Restart Policy**: `Unless Stopped`

4. Click **Apply** to create and start the container

### Unraid WebUI Access

After starting the container, access Madtorio from Unraid:
- **From Unraid Dashboard**: Click the Madtorio icon → **WebUI**
- **Direct URL**: `http://UNRAID_IP:8083`
- **From other devices**: `http://UNRAID_IP:8083`

## Volume Management

### Directory Structure

The persistent data volume (`/ap./AppData`) contains:

```
/ap./AppData/
├── madtorio.db          # SQLite database (< 100MB typical)
├── madtorio.db-shm      # SQLite shared memory file
├── madtorio.db-wal      # SQLite write-ahead log
├── keys/                # Data protection keys (authentication cookies)
└── uploads/
    ├── saves/           # Factorio save files (grows with uploads)
    └── temp/            # Temporary chunked upload files
```

> **Important**: The `keys/` directory stores encryption keys for authentication cookies. If deleted, all users will be logged out and need to re-authenticate.

### Backup Strategy

#### Full Backup (Recommended)

Backup the entire data directory:

```bash
# On host machine
tar -czf madtorio-backup-$(date +%Y%m%d).tar.gz /mnt/user/appAppData/madtori./AppData/

# Or on Unraid
tar -czf /mnt/user/backups/madtorio-backup-$(date +%Y%m%d).tar.gz /mnt/user/appAppData/madtori./AppData/
```

#### Selective Backup

Backup only essential data (exclude temp files):

```bash
# Database only
cp /mnt/user/appAppData/madtorio/AppData/madtorio.db /mnt/user/backups/

# Database + saves (exclude temp)
rsync -av --exclude='temp/' /mnt/user/appAppData/madtori./AppData/ /mnt/user/backups/madtorio-AppData/
```

### Restore from Backup

```bash
# Stop container
docker compose down

# Restore data
tar -xzf madtorio-backup-20260117.tar.gz -C /

# Or for Unraid
tar -xzf /mnt/user/backups/madtorio-backup-20260117.tar.gz -C /mnt/user/appAppData/madtorio/

# Start container
docker compose up -d
```

### Storage Sizing

- **Database**: ~150KB initial, grows to ~100MB max (typical)
- **Save Files**: 500MB per file maximum, no limit on count
- **Recommended**: 50GB+ for medium usage (10-50 save files)

## Security

### Default Credentials Warning

**CRITICAL**: The default credentials (`admin@madtorio.com` / `Madtorio2026!`) are publicly known. You **MUST** change them before deploying to production.

### Changing Admin Credentials

#### Option 1: Environment Variables (Before First Run)

Edit `.env` file before starting container:
```bash
ADMIN_EMAIL=youremail@example.com
ADMIN_PASSWORD=YourNewSecurePassword123!
```

#### Option 2: In-Application (After First Run)

1. Log in with existing admin credentials
2. Navigate to user profile settings
3. Change email and password
4. Log out and log back in with new credentials

### HTTPS Configuration

For production deployments, use a reverse proxy for HTTPS:

#### Nginx Reverse Proxy Example:

```nginx
server {
    listen 443 ssl http2;
    server_name madtorio.yourdomain.com;

    ssl_certificate /path/to/cert.pem;
    ssl_certificate_key /path/to/key.pem;

    location / {
        proxy_pass http://localhost:8083;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;

        # Important for large file uploads
        proxy_read_timeout 600s;
        proxy_send_timeout 600s;
        client_max_body_size 500M;
    }
}
```

#### Traefik Example (Docker Labels):

Add to `docker-compose.yml`:
```yaml
labels:
  - "traefik.enable=true"
  - "traefik.http.routers.madtorio.rule=Host(`madtorio.yourdomain.com`)"
  - "traefik.http.routers.madtorio.entrypoints=websecure"
  - "traefik.http.routers.madtorio.tls.certresolver=letsencrypt"
  - "traefik.http.services.madtorio.loadbalancer.server.port=8080"
```

### File Permissions

The container runs as the built-in `app` user (UID 1654) from Microsoft's .NET base image. Ensure host volume has appropriate permissions:

```bash
# On Unraid or Linux host
chown -R 1654:1654 /mnt/user/appAppData/madtori./AppData
chmod -R 755 /mnt/user/appAppData/madtori./AppData
```

## Troubleshooting

### Container Won't Start

**Check logs:**
```bash
docker compose logs -f madtorio
# Or on Unraid
docker logs madtorio
```

**Common issues:**
1. **Port conflict**: Port 8083 already in use
   - Solution: Change host port in `docker-compose.yml`
   ```yaml
   ports:
     - "8084:8080"  # Use 8084 instead
   ```

2. **Permission denied**: Volume mount permission issues
   - Solution: Fix ownership
   ```bash
   chown -R 1654:1654 /mnt/user/appAppData/madtori./AppData
   ```

3. **Database locked**: Another process accessing database
   - Solution: Stop all Madtorio containers, remove `madtorio.db-shm` and `madtorio.db-wal`, restart

### File Upload Fails

1. **Check volume mount**: Ensure `/ap./AppData` is writable
   ```bash
   docker exec -it madtorio ls -la /ap./AppData/uploads/
   ```

2. **Check disk space**: Ensure host has enough free space
   ```bash
   df -h /mnt/user/appAppData/madtorio/
   ```

3. **Check file size**: Files over 500MB are rejected
   - Current limit: 500MB per file
   - To change: Modify `MaxFileSize` in services and rebuild

### Can't Access WebUI

1. **Verify container is running**:
   ```bash
   docker ps | grep madtorio
   ```

2. **Check port binding**:
   ```bash
   docker port madtorio
   # Should show: 8080/tcp -> 0.0.0.0:8083
   ```

3. **Test from host**:
   ```bash
   curl http://localhost:8083
   ```

4. **Check firewall**: Ensure port 8083 is open on host

### Database Corruption

If database becomes corrupted:

1. **Stop container**:
   ```bash
   docker compose down
   ```

2. **Restore from backup**:
   ```bash
   cp /mnt/user/backups/madtorio.db /mnt/user/appAppData/madtori./AppData/
   ```

3. **Or rebuild database** (loses all data):
   ```bash
   rm /mnt/user/appAppData/madtorio/AppData/madtorio.db*
   docker compose up -d
   # Database will be recreated with default admin user
   ```

## Updating

### Update Container Image

1. **Pull latest code**:
   ```bash
   cd /mnt/user/appAppData/madtorio
   git pull origin main
   ```

2. **Rebuild image**:
   ```bash
   docker compose build --no-cache
   ```

3. **Restart container**:
   ```bash
   docker compose down
   docker compose up -d
   ```

4. **Verify update**:
   ```bash
   docker compose logs -f
   ```

### Database Migrations

Database migrations run automatically on startup. No manual intervention needed.

## Data Migration

### From Development to Production

1. **Export database** from development:
   ```bash
   # On dev machine
   cp madtorio.db madtorio-export.db
   ```

2. **Copy to production**:
   ```bash
   # On Unraid
   docker compose down
   cp madtorio-export.db /mnt/user/appAppData/madtorio/AppData/madtorio.db
   docker compose up -d
   ```

3. **Migrate save files**:
   ```bash
   # Copy App_Data/uploads/saves/* to AppData/uploads/saves/
   cp -r App_Data/uploads/saves/* /mnt/user/appAppData/madtori./AppData/uploads/saves/
   ```

### Between Docker Hosts

1. **Create backup** on source:
   ```bash
   tar -czf madtorio-migration.tar.gz -C /mnt/user/appAppData/madtori./AppData .
   ```

2. **Transfer to destination**:
   ```bash
   scp madtorio-migration.tar.gz user@new-host:/tmp/
   ```

3. **Extract on destination**:
   ```bash
   # On new host
   mkdir -p /mnt/user/appAppData/madtori./AppData
   tar -xzf /tmp/madtorio-migration.tar.gz -C /mnt/user/appAppData/madtori./AppData/
   chown -R 1654:1654 /mnt/user/appAppData/madtori./AppData
   ```

4. **Start container** on new host

## Performance Tuning

### SQLite Optimization

For better performance with many concurrent users, consider:

1. **Enable WAL mode** (already enabled by default):
   ```sql
   PRAGMA journal_mode=WAL;
   ```

2. **Increase cache size** (in connection string):
   ```
   DataSource=/app/AppData/madtorio.db;Cache=Shared;Page Size=4096;Cache Size=10000
   ```

### File Storage

- **SSD recommended** for database and temp uploads
- **HDD acceptable** for final save files
- **Network storage**: Works but may impact upload performance

## Support

- **Issues**: [GitHub Issues](https://github.com/yourusername/madtorio/issues)
- **Documentation**: [Main README](README.md)
- **CLAUDE.md**: [Development Guide](CLAUDE.md)

## License

[Your License Here]
