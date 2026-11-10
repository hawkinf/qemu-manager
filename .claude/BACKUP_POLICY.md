# Backup Policy

## Schedule
- **Daily:** incremental backup at 02:00
- **Weekly (Sunday):** full backup
- **Retention:** last 15 backups kept, older deleted automatically

## What to Back Up
- Database (PostgreSQL dump / Firestore export)
- User-uploaded files (Storage)
- Config files (.env, nginx conf, SSL certs)
- Application code (git repo = primary backup)

## Storage
- Primary: VPS local (`/backups/`)
- Secondary: Google Drive (automated sync)
- Git repos: GitHub (always push before backup)

## Restore Procedure
1. Stop the service
2. Restore database from latest dump
3. Restore files from backup archive
4. Restart service
5. Run smoke test

## Verification
- After each backup: verify file size > 0
- Weekly: test restore to staging environment
- Monthly: full restore drill

## Alerts
- If backup fails → log to `/var/log/backup.log` + notify via email/Telegram
