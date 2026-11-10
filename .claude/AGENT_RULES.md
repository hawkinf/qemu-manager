# Agent Rules — Full Pipeline Reference

## Incremental Development

Every task must be broken into steps of 5–15 min max.
Execute one step. Commit. Stop. Wait for next command.

## Pipeline

| Phase | Action |
|---|---|
| 1. Diagnose | Read only what is needed. Identify root cause. List impact. |
| 2. Fix | Smallest safe change. No unrelated refactors. |
| 3. Validate | Run analyzer + tests. Fix issues before proceeding. |
| 4. Harden | Add error handling. Clear log messages. Safe fallbacks. |
| 5. Integrate | Verify sync with backend/Firebase if applicable. |
| 6. UX | Loading states, empty states, user-facing messages. |
| 7. Commit | Format: `<AREA>: <type> - <description>` |

## Anti-Regression Rules

- Never break existing functionality to fix something new
- Always run full test suite before committing
- If a change touches shared/core code — extra caution, read all callers
- When in doubt, do less and ask

## What NOT to do

- Do not refactor code that was not asked to change
- Do not add features beyond the current step
- Do not add comments/docstrings unless asked
- Do not expose stack traces to users
- Do not leave TODO/FIXME/print/Console.WriteLine in committed code
