# Debug Playbook

## Step 1 — Reproduce
- Confirm the exact steps that trigger the issue
- Note: device, OS, version, user role

## Step 2 — Isolate
- Identify which module/layer fails (UI / business logic / data / network)
- Read logs: Crashlytics, Firebase, server logs, flutter logs

## Step 3 — Root Cause
- Find the actual failing line, not just the symptom
- Check: null safety, async races, missing await, type mismatch

## Step 4 — Minimal Fix
- Change as little as possible
- Do not refactor surrounding code
- Add a log line if needed to confirm the fix path

## Step 5 — Validate
- Reproduce the original issue — it must be gone
- Run all tests — nothing new broke

## Step 6 — Harden
- Add guard/fallback if edge case could recur
- Add structured log at the fix point

## Common Patterns

| Symptom | First place to look |
|---|---|
| Null crash | Null safety operators, async timing |
| 401/403 | Auth token expiry, missing header |
| Data not showing | Provider not watching, stream closed |
| Sync not working | Offline queue, Firestore rules |
| Build fails | pubspec.lock conflict, missing env var |
