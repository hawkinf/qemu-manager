# Hawk Informatica Pro — Rollback Policy

## Objective
Ensure failed releases can be reversed safely and predictably.

## When Rollback Is Required
- Critical flow broken after release
- Authentication fails in production
- Data corruption risk detected
- Deployment causes startup failure
- Severe performance degradation
- Wrong environment/config deployed

## Rollback Principles
- Prefer a known good version.
- Do not improvise under pressure.
- Understand data impact before rollback.
- Validate after rollback.
- Document the incident after stabilization.

## Pre-Rollback Checklist
- Identify exact failed version
- Identify last known good version
- Confirm whether DB/config changes were included
- Check whether rollback is code-only or data-sensitive
- Preserve logs/evidence before changing state

## Rollback Procedure
1. Stop further risky deployments.
2. Identify rollback target.
3. Restore previous artifact/version.
4. Revert config/env changes if needed.
5. Address DB rollback carefully (confirm with user first).
6. Validate critical flows.
7. Monitor logs/errors.
8. Document outcome.

## Database Rollback Precautions
- Do not assume DB rollback is safe.
- Check whether migrations are reversible.
- Prefer forward-fix when rollback would risk data loss.
- If restoring from backup, confirm recovery point and business impact.

## Post-Rollback Verification
- Application starts correctly.
- Auth works.
- Main flows work.
- Logs are stable.
- Monitoring reflects recovery.

## Incident Logging
Record: root cause, impacted version, rollback version, data impact, follow-up actions.
