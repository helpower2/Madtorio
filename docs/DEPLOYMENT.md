# Madtorio Deployment Guide

This document covers deployment configurations, data storage, and production setup for Madtorio.

## Table of Contents

- [Data Storage](#data-storage)
- [Docker Deployment](#docker-deployment)
- [Unraid Deployment](#unraid-deployment)
- [Production Configuration](#production-configuration)
- [Backup and Recovery](#backup-and-recovery)

---

## Data Storage

### Overview

Madtorio uses different data storage locations depending on the environment.

### Development Environment

**Database Location**:
- File: `Data/madtorio.db` (or `data/madtorio.db` - same location on Windows)
- Includes WAL files: `madtorio.db-shm`, `madtorio.db-wal`
- Git-ignored via `.gitignore`

**Uploaded Save Files**:
- Directory: `data/uploads/saves/`
- Contains all uploaded Factorio save files

**Temporary Upload Chunks**:
- Directory: `data/uploads/temp/`
- Temporary storage during chunked uploads
- Automatically cleaned up after assembly

**Data Protection Keys**:
- Directory: `data/keys/`
- ASP.NET Core Data Protection keys for authentication
- Critical for cookie encryption

**Note**: All runtime data directories (`data/`) are git-ignored.

### Production/Docker Environment

All data is consolidated in `/app/data/` within the container:

**Database**:
- `/app/data/madtorio.db`
- `/app/data/madtorio.db-shm`
- `/app/data/madtorio.db-wal`

**Uploaded Save Files**:
- `/app/data/uploads/saves/`

**Temporary Upload Chunks**:
- `/app/data/uploads/temp/`

**Data Protection Keys**:
- `/app/data/keys/`

### Directory Name Note

**Important**: The `Data/` directory (uppercase) in the repository contains **source code** (Models, Migrations, Seed classes). The `data/` directory (lowercase) is created at runtime for **data storage**.

On Windows (case-insensitive filesystem), both paths resolve to the same directory name but serve completely different purposes:
- `Data/` → Source code
- `data/` → Runtime data

On Linux (case-sensitive filesystem), these are distinct directories.

---

## Docker Deployment

### Docker Volume Mapping

To persist data across container restarts, mount the `/app/data` directory:

#### docker-compose.yml Example

```yaml
services:
  madtorio:
    image: helpower2/madtorio:latest
    container_name: madtorio
    ports:
      - "8083:8080"
    volumes:
      - ./data:/app/data
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - AdminEmail=admin@madtorio.com
      - AdminPassword=YourSecurePassword123!
    restart: unless-stopped
```

#### Docker Run Command

```bash
docker run -d \
  --name madtorio \
  -p 8083:8080 \
  -v $(pwd)/data:/app/data \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e AdminEmail=admin@madtorio.com \
  -e AdminPassword=YourSecurePassword123! \
  --restart unless-stopped \
  helpower2/madtorio:latest
```

### Volume Path Recommendations

**Development/Docker Compose**:
- Relative path: `./data:/app/data`
- Full path: `/path/to/madtorio/data:/app/data`

**Production/Unraid**:
- Recommended: `/mnt/user/appdata/madtorio:/app/data`

### Data Persistence Warning

**CRITICAL**: Without a volume mount, all data is **ephemeral** and will be **lost** when:
- Container is removed
- Container is updated
- Host is rebooted (unless restart policy set)

Always mount `/app/data` to a persistent location on the host.

---

## Unraid Deployment

### Template Installation

#### Method 1: Community Applications (Recommended)

1. Install "Community Applications" plugin (if not already installed)
2. Search for "Madtorio" in Community Applications
3. Click "Install"
4. Configure settings in the template
5. Click "Apply"

#### Method 2: Manual Template Import

1. Download `unraid-template.xml` from repository
2. In Unraid web UI, go to **Docker** tab
3. Click **Add Container**
4. Click **Template** dropdown → **Import Template**
5. Upload `unraid-template.xml`
6. Configure settings
7. Click **Apply**

### Volume Mapping

**Container Path**: `/app/data`
**Host Path**: `/mnt/user/appdata/madtorio` (recommended)

This mapping persists:
- SQLite database (`madtorio.db`)
- Uploaded save files
- Data Protection keys
- Temporary upload chunks

### Port Configuration

**Default**: 8567
**Container Port**: 8080 (internal, do not change)
**Host Port**: 8567 (can be changed if port conflict)

### First-Time Setup

1. Deploy the container from template
2. Access web UI at `http://[UNRAID-IP]:8567`
3. Log in with credentials:
   - **Email**: Value from `AdminEmail` template variable (default: admin@madtorio.com)
   - **Password**: Value from `AdminPassword` template variable or check container logs

### Environment Variables in Template

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `AdminEmail` | Initial admin email | `admin@madtorio.com` | Yes |
| `AdminPassword` | Initial admin password | Auto-generated | Yes |
| `ASPNETCORE_ENVIRONMENT` | Application environment | `Production` | No |

### Data Backup on Unraid

Backup the appdata directory to preserve all data:

```bash
# Full backup
tar -czf /mnt/user/backups/madtorio-backup-$(date +%Y%m%d).tar.gz \
  /mnt/user/appdata/madtorio/

# Restore from backup
tar -xzf /mnt/user/backups/madtorio-backup-20260117.tar.gz \
  -C /mnt/user/appdata/madtorio/
```

### Troubleshooting Unraid

#### Data Not Persisting

**Symptom**: Data disappears after container restart

**Solution**:
1. Check volume mapping in container settings
2. Verify `/mnt/user/appdata/madtorio/` exists
3. Run: `docker exec madtorio ls -la /app/data`
4. Ensure proper permissions: `chown -R 1654:1654 /mnt/user/appdata/madtorio`

#### Cannot Access Web UI

**Solution**:
1. Check container is running: `docker ps | grep madtorio`
2. Check logs: `docker logs madtorio`
3. Verify port not blocked by firewall
4. Test from Unraid server: `curl http://localhost:8567`

---

## Production Configuration

### Environment Variables

Set via environment variables or `.env` file:

```bash
ASPNETCORE_ENVIRONMENT=Production
AdminEmail=admin@madtorio.com
AdminPassword=YourSecurePassword123!
ConnectionStrings__DefaultConnection=DataSource=/app/data/madtorio.db;Cache=Shared
```

### appsettings.Production.json

Production-specific configuration:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "DataSource=/app/data/madtorio.db;Cache=Shared"
  }
}
```

### Security Considerations

#### Change Default Credentials

The default admin credentials are **publicly known**. You **MUST** change them:

**Option 1: Before First Run**
```bash
# Set via environment variables
export AdminEmail=youremail@example.com
export AdminPassword=YourNewSecurePassword123!
```

**Option 2: After First Run**
1. Log in with default credentials
2. Navigate to user profile settings
3. Change email and password
4. Log out and log back in

#### HTTPS with Reverse Proxy

For production, use a reverse proxy for HTTPS:

**Nginx Example**:
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

**Traefik Example** (Docker labels):
```yaml
labels:
  - "traefik.enable=true"
  - "traefik.http.routers.madtorio.rule=Host(`madtorio.yourdomain.com`)"
  - "traefik.http.routers.madtorio.entrypoints=websecure"
  - "traefik.http.routers.madtorio.tls.certresolver=letsencrypt"
  - "traefik.http.services.madtorio.loadbalancer.server.port=8080"
```

### File Permissions

The container runs as non-root user (UID 1654). Ensure host volume has correct permissions:

```bash
# On Linux/Unraid
chown -R 1654:1654 /mnt/user/appdata/madtorio
chmod -R 755 /mnt/user/appdata/madtorio
```

---

## Backup and Recovery

### What to Backup

Essential data to backup:
1. **Database**: `madtorio.db` (includes WAL files)
2. **Save Files**: `data/uploads/saves/`
3. **Data Protection Keys**: `data/keys/`

Optional (can be regenerated):
- Temporary chunks: `data/uploads/temp/`

### Backup Strategies

#### Full Backup (Recommended)

Backup entire data directory:

```bash
# On host machine
tar -czf madtorio-backup-$(date +%Y%m%d).tar.gz /path/to/data/

# On Unraid
tar -czf /mnt/user/backups/madtorio-backup-$(date +%Y%m%d).tar.gz \
  /mnt/user/appdata/madtorio/
```

#### Database Only

Backup just the database:

```bash
# Stop container first
docker compose down

# Backup database
cp /path/to/data/madtorio.db /path/to/backup/

# Restart container
docker compose up -d
```

#### Automated Backup (Cron)

Create automated daily backups:

```bash
# Add to crontab
0 2 * * * tar -czf /mnt/user/backups/madtorio-$(date +\%Y\%m\%d).tar.gz \
  /mnt/user/appdata/madtorio/ && \
  find /mnt/user/backups/madtorio-*.tar.gz -mtime +30 -delete
```

This backs up daily at 2 AM and keeps 30 days of backups.

### Restore from Backup

```bash
# Stop container
docker compose down

# Restore data
tar -xzf madtorio-backup-20260117.tar.gz -C /path/to/restore/

# Fix permissions (if needed)
chown -R 1654:1654 /path/to/data/

# Start container
docker compose up -d
```

### Data Migration

#### Between Docker Hosts

1. **Create backup on source**:
   ```bash
   tar -czf madtorio-migration.tar.gz -C /path/to/data .
   ```

2. **Transfer to destination**:
   ```bash
   scp madtorio-migration.tar.gz user@new-host:/tmp/
   ```

3. **Extract on destination**:
   ```bash
   mkdir -p /mnt/user/appdata/madtorio
   tar -xzf /tmp/madtorio-migration.tar.gz -C /mnt/user/appdata/madtorio/
   chown -R 1654:1654 /mnt/user/appdata/madtorio/
   ```

4. **Start container on new host**

#### From Development to Production

1. **Export from development**:
   ```bash
   cp madtorio.db madtorio-export.db
   ```

2. **Copy to production**:
   ```bash
   docker compose down
   cp madtorio-export.db /path/to/data/madtorio.db
   docker compose up -d
   ```

3. **Migrate save files**:
   ```bash
   cp -r local-data/uploads/saves/* /path/to/data/uploads/saves/
   ```

### Storage Requirements

- **Database**: ~150KB initial, grows to ~100MB typical
- **Save Files**: 500MB maximum per file, no limit on count
- **Recommended**: 50GB+ for medium usage (10-50 save files)

---

## Performance Tuning

### SQLite Optimization

For better performance with concurrent users:

1. **WAL mode** (already enabled by default):
   ```sql
   PRAGMA journal_mode=WAL;
   ```

2. **Increase cache size** (in connection string):
   ```
   DataSource=/app/data/madtorio.db;Cache=Shared;Page Size=4096;Cache Size=10000
   ```

### File Storage Performance

- **SSD recommended** for database and temp uploads
- **HDD acceptable** for final save files in `uploads/saves/`
- **Network storage**: Works but may impact upload performance

### Container Resources

Recommended Docker resource limits:

```yaml
services:
  madtorio:
    # ... other config
    deploy:
      resources:
        limits:
          cpus: '2.0'
          memory: 1G
        reservations:
          cpus: '0.5'
          memory: 512M
```

---

## Monitoring and Maintenance

### Container Logs

View application logs:

```bash
# Docker Compose
docker compose logs -f madtorio

# Docker
docker logs -f madtorio

# Unraid
docker logs madtorio
```

### Health Checks

Check container health:

```bash
docker inspect madtorio | grep -A 10 Health
```

### Disk Usage

Monitor disk usage:

```bash
# Check data directory size
du -sh /mnt/user/appdata/madtorio/

# Check specific subdirectories
du -sh /mnt/user/appdata/madtorio/uploads/saves/
du -sh /mnt/user/appdata/madtorio/uploads/temp/
```

### Maintenance Tasks

#### Clean Temporary Files

Remove old temporary chunks:

```bash
find /path/to/data/uploads/temp/ -type f -mtime +7 -delete
```

#### Vacuum Database

Optimize database (do this during low traffic):

```bash
docker exec -it madtorio sqlite3 /app/data/madtorio.db "VACUUM;"
```

---

## Additional Resources

- [README.Docker.md](../README.Docker.md) - Comprehensive Docker deployment guide
- [CICD-SETUP.md](../CICD-SETUP.md) - CI/CD pipeline configuration
- [Docker Documentation](https://docs.docker.com/)
- [Unraid Documentation](https://wiki.unraid.net/)

## Getting Help

- Review logs: `docker logs madtorio`
- Check [ARCHITECTURE.md](ARCHITECTURE.md) for system design
- Check [DEVELOPMENT.md](DEVELOPMENT.md) for development setup
- Create GitHub issue with logs and error details
