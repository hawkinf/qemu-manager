# Hawk Informatica Pro — CI/CD Standard

## Objective
Define a minimum reliable CI/CD baseline for all repositories.

## Required CI Stages
1. Dependency install/restore
2. Static validation (lint/analyze/typecheck)
3. Tests
4. Build
5. Artifact generation
6. Optional deployment
7. Post-deploy smoke checks

## Core Rules
- Do not deploy if validation fails.
- Protect the main branch.
- Use versioned artifacts whenever possible.
- Keep CI/CD deterministic and explicit.
- Maintain a rollback path.

## Language-Specific

### Python
```bash
pip install -r requirements.txt
pytest
ruff check .
mypy .
```

### Flutter/Dart
```bash
flutter pub get
flutter analyze        # 0 issues required
flutter test           # all passing required
flutter build windows / apk / web
```

### C#
```bash
dotnet restore
dotnet build           # 0 errors required
dotnet test
dotnet publish -c Release
```

## Smoke Check (post-deploy)
- Application/service starts
- Auth works
- One critical happy path works
- No obvious crash loop or major log spike

## Branch Strategy
- `main` — stable, deployable
- Feature branches for work in progress
- No direct unvalidated push to main
