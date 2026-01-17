# CI/CD Setup Instructions

This document provides step-by-step instructions for setting up the CI/CD pipeline for Madtorio.

## Overview

The CI/CD pipeline uses GitHub Actions to automatically build, test, and deploy the Madtorio application to Docker Hub. The pipeline is configured to:

- **Feature branches**: Run CI validation (build + tests)
- **Main branch**: Build Docker images and push to Docker Hub
- **Version tags**: Create GitHub releases with semantic versioning

## Prerequisites

- GitHub repository access
- Docker Hub account
- Unraid server (for deployment)

## Step 1: Create Docker Hub Repository

1. Login to [Docker Hub](https://hub.docker.com)
2. Click **Create Repository**
3. Configure repository:
   - **Name**: `madtorio`
   - **Visibility**: **Private** (important!)
   - **Description**: "Blazor Server application for managing Factorio save files"
4. Click **Create**

Your Docker Hub repository URL will be: `https://hub.docker.com/r/helpower2/madtorio`

## Step 2: Generate Docker Hub Access Token

1. Go to Docker Hub → **Account Settings** → **Security**
2. Click **New Access Token**
3. Configure token:
   - **Access Token Description**: "GitHub Actions - Madtorio CI/CD"
   - **Access permissions**: **Read, Write, Delete**
4. Click **Generate**
5. **IMPORTANT**: Copy the token immediately (you won't be able to see it again)

## Step 3: Configure GitHub Secrets

1. Go to your GitHub repository: `https://github.com/helpower2/Madtorio`
2. Navigate to **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret** for each of the following:

### Required Secrets

| Secret Name | Value | Description |
|-------------|-------|-------------|
| `DOCKERHUB_USERNAME` | `helpower2` | Your Docker Hub username |
| `DOCKERHUB_TOKEN` | `[token from Step 2]` | Docker Hub access token |

### Optional Secrets (Future Use)

| Secret Name | Value | Description |
|-------------|-------|-------------|
| `DISCORD_WEBHOOK_URL` | `[webhook URL]` | Discord webhook for deployment notifications |

## Step 4: Configure Branch Protection (Recommended)

1. Go to **Settings** → **Branches**
2. Click **Add branch protection rule**
3. Configure for branch `main`:
   - ✅ **Require a pull request before merging**
     - Require approvals: `1`
   - ✅ **Require status checks to pass before merging**
     - Add: `Build and Test` (from CI workflow)
   - ✅ **Require branches to be up to date before merging**
   - ✅ **Do not allow bypassing the above settings**
4. Scroll down and click **Create**

### Auto-delete Head Branches

1. Go to **Settings** → **General**
2. Scroll to **Pull Requests**
3. ✅ Enable **Automatically delete head branches**
4. Click **Save**

## Step 5: Configure Unraid for Private Docker Registry

Since the Docker Hub repository is private, Unraid needs credentials to pull images.

### Option A: Docker Compose Method

1. SSH into your Unraid server:
   ```bash
   ssh root@your-unraid-ip
   ```

2. Login to Docker Hub:
   ```bash
   docker login
   # Username: helpower2
   # Password: [use the access token from Step 2]
   ```

3. Verify login:
   ```bash
   docker pull helpower2/madtorio:latest
   ```

### Option B: Using Unraid Docker Template

1. In Unraid web interface, go to **Docker** tab
2. Click **Add Container**
3. Enable **Advanced View** (top right)
4. Add **Registry URL**: `https://index.docker.io/v1/`
5. Add **Registry Username**: `helpower2`
6. Add **Registry Password**: `[access token from Step 2]`
7. Configure other settings as usual

## Step 6: Test the CI/CD Pipeline

### Test Feature Branch CI

1. Create a test branch:
   ```bash
   git checkout -b feature/test-cicd
   ```

2. Make a small change (e.g., edit README.md)

3. Commit and push:
   ```bash
   git add .
   git commit -m "test: Verify CI/CD pipeline"
   git push origin feature/test-cicd
   ```

4. Check GitHub Actions:
   - Go to **Actions** tab in GitHub
   - Verify "CI - Feature Branch" workflow runs successfully
   - Should see green checkmark ✅

### Test Pull Request Checks

1. Create a pull request from `feature/test-cicd` to `main`
2. Verify PR checks run automatically
3. Should see automated PR comment with status
4. Do **NOT** merge yet (just testing)

### Test Main Branch CD

1. Merge the test PR to main (or push directly to main if branch protection not enabled)

2. Check GitHub Actions:
   - Verify "CD - Main Branch" workflow runs
   - Should build Docker image
   - Should push to Docker Hub

3. Verify Docker Hub:
   - Go to `https://hub.docker.com/r/helpower2/madtorio/tags`
   - Should see new tags:
     - `latest`
     - `stable`
     - `main-[short-sha]`

4. Test pull on Unraid:
   ```bash
   docker pull helpower2/madtorio:latest
   docker images | grep madtorio
   ```

## Step 7: Create First Release

Once the CI/CD pipeline is working, create your first release:

```bash
# Make sure you're on main branch and up to date
git checkout main
git pull

# Create and push a version tag
git tag -a v1.0.0 -m "Release version 1.0.0"
git push origin v1.0.0
```

This will trigger the CD workflow to:
1. Build Docker image
2. Tag with version numbers: `v1.0.0`, `v1.0`, `v1`, `latest`, `stable`
3. Push to Docker Hub
4. Create GitHub release with changelog

Verify the release:
- GitHub: `https://github.com/helpower2/Madtorio/releases`
- Docker Hub: `https://hub.docker.com/r/helpower2/madtorio/tags`

## Workflow Summary

### Daily Development Workflow

```bash
# 1. Create feature branch
git checkout main
git pull
git checkout -b feature/my-feature

# 2. Make changes
# ... edit files ...

# 3. Commit and push
git add .
git commit -m "feat: Add new feature"
git push origin feature/my-feature

# 4. CI runs automatically (build + test)

# 5. Create PR when ready
gh pr create --title "Add new feature" --body "Description"

# 6. PR checks run automatically

# 7. After review and approval, merge to main

# 8. CD runs automatically:
#    - Builds Docker image
#    - Pushes to Docker Hub
#    - Unraid auto-updates at 01:00
```

### Creating a Release

```bash
# 1. Ensure main is ready
git checkout main
git pull

# 2. Tag with semantic version
git tag -a v1.1.0 -m "Release version 1.1.0 - Add user roles"
git push origin v1.1.0

# 3. GitHub Actions automatically:
#    - Builds Docker image
#    - Creates release
#    - Tags: v1.1.0, v1.1, v1, latest
```

### Hotfix Workflow

```bash
# 1. Create hotfix branch from main
git checkout main
git pull
git checkout -b hotfix/critical-bug

# 2. Fix the issue
# ... edit files ...

# 3. Commit and push
git add .
git commit -m "fix: Resolve critical bug"
git push origin hotfix/critical-bug

# 4. Create PR and merge quickly

# 5. Tag a patch version
git tag -a v1.0.1 -m "Hotfix version 1.0.1"
git push origin v1.0.1
```

## Deployment

### Automatic Deployment (Unraid)

Unraid is configured to auto-update daily at 01:00. No manual action required.

To check update schedule:
```bash
# On Unraid
crontab -l | grep docker
```

### Manual Deployment (Unraid)

To deploy immediately without waiting:

```bash
# SSH into Unraid
ssh root@your-unraid-ip

# Navigate to Madtorio directory
cd /mnt/user/appdata/madtorio

# Pull latest image
docker compose pull

# Restart container
docker compose up -d --force-recreate

# View logs
docker compose logs -f madtorio
```

### Rollback Procedure

If a deployment has issues, rollback to previous version:

```bash
# Option 1: Rollback to specific version
docker compose down
# Edit docker-compose.yml, change:
# image: helpower2/madtorio:latest
# to:
# image: helpower2/madtorio:v1.0.0
docker compose up -d

# Option 2: Quick rollback via git revert
# On development machine:
git checkout main
git revert HEAD
git push origin main
# CI rebuilds and pushes fixed version
# Unraid auto-pulls at 01:00 or manually pull
```

## Monitoring

### GitHub Actions Status

- View workflow runs: `https://github.com/helpower2/Madtorio/actions`
- Status badges in README.md show current build status
- Email notifications on workflow failures (configured in GitHub settings)

### Docker Hub

- View tags: `https://hub.docker.com/r/helpower2/madtorio/tags`
- Check image size and metadata
- View pull statistics

### Unraid

```bash
# Check container status
docker ps | grep madtorio

# View logs
docker logs madtorio

# Check health
docker inspect madtorio | grep -A 10 Health
```

## Troubleshooting

### "docker: pull access denied" on Unraid

**Problem**: Unraid can't pull from private Docker Hub repository

**Solution**:
```bash
# SSH into Unraid
docker login
# Enter: helpower2
# Password: [Docker Hub access token]

# Verify
docker pull helpower2/madtorio:latest
```

### GitHub Actions: "Invalid username or password"

**Problem**: Docker Hub login fails in workflow

**Solution**:
1. Verify `DOCKERHUB_USERNAME` secret is exactly `helpower2`
2. Regenerate Docker Hub access token
3. Update `DOCKERHUB_TOKEN` secret in GitHub

### Workflow: "Resource not accessible by integration"

**Problem**: GitHub Actions can't create releases or comments

**Solution**:
1. Go to **Settings** → **Actions** → **General**
2. Scroll to **Workflow permissions**
3. Select **Read and write permissions**
4. ✅ **Allow GitHub Actions to create and approve pull requests**
5. Click **Save**

### Tests failing in CI but passing locally

**Problem**: Tests work locally but fail in GitHub Actions

**Solution**:
1. Check .NET version matches (10.0.x)
2. Verify all dependencies are in `.csproj`
3. Check for environment-specific issues
4. Run locally with `dotnet test --configuration Release`

### Docker build fails: "version not found"

**Problem**: Build argument VERSION not being passed

**Solution**:
- Check `cd-main.yml` workflow file
- Ensure `build-args: VERSION=${{ steps.meta.outputs.version }}` is present
- Verify Dockerfile has `ARG VERSION=dev`

## Security Best Practices

- ✅ Use Docker Hub **access tokens**, not passwords
- ✅ Keep tokens as GitHub **Secrets**, never commit them
- ✅ Use **private** Docker Hub repository
- ✅ Enable **branch protection** on main
- ✅ Require **PR reviews** before merging
- ✅ Enable **Dependabot** for automated security updates
- ✅ Run containers as **non-root** user (UID 1654)
- ✅ Regularly update base Docker images

## Maintenance

### Weekly Tasks
- Review Dependabot PRs
- Check for security vulnerabilities
- Review GitHub Actions usage

### Monthly Tasks
- Review and cleanup old Docker Hub tags
- Check Unraid logs for issues
- Review CI/CD performance metrics

### Quarterly Tasks
- Rotate Docker Hub access tokens
- Review and update documentation
- Audit GitHub Actions workflows

## Additional Resources

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Docker Hub Documentation](https://docs.docker.com/docker-hub/)
- [Semantic Versioning](https://semver.org/)
- [Conventional Commits](https://www.conventionalcommits.org/)

## Support

For issues with the CI/CD pipeline:
1. Check workflow logs in GitHub Actions
2. Review this documentation
3. Create an issue on GitHub with logs and error messages

---

**Setup Date**: January 2026
**Last Updated**: January 17, 2026
**Maintained By**: helpower2
