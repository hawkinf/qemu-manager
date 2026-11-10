# Release Flow

## Phases

### R1 — Prepare
- Bump version (pubspec.yaml / pyproject.toml / .csproj)
- Update CHANGELOG

### R2 — Validate
- Static analyzer: 0 issues
- All tests pass
- Manual smoke test on main flow

### R3 — Build

**Flutter:**
```bash
flutter build windows
flutter build apk --release
flutter build web
```

**Python:**
```bash
pip install -e .
python -m build
```

**C#:**
```bash
dotnet build -c Release
dotnet publish -c Release
```

### R4 — Deploy (if applicable)
- Backend: deploy API, validate endpoints
- Firebase: `firebase deploy`
- VPS: rsync / SSH upload

### R5 — Post-Deploy Checks
- Login flow works
- Main feature works
- Error tracking active (Crashlytics/Sentry)
- No 5xx in logs

### R6 — Tag
```bash
git tag vX.X.X
git push origin vX.X.X
```

## Release Notes Template

```
Version: vX.X.X

New:
- ...

Fixes:
- ...

Improvements:
- ...
```
