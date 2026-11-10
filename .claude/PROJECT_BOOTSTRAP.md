# Hawk Informatica Pro — Project Bootstrap

## Objective
Default starting structure and behavior for new or existing repositories.

## Universal Bootstrap Rules
- Create `.claude/` first in any new repo.
- Load `.claude/AGENT_RULES.md` before work begins.
- Define how to run validation locally.
- Define release expectations early.
- Avoid undocumented environment assumptions.

## New Repository Checklist
- [ ] Language and runtime confirmed
- [ ] Build/test commands documented
- [ ] Folder structure clear
- [ ] Environment variable strategy defined
- [ ] Release path defined
- [ ] Backup/rollback expectations set

## Python Bootstrap
```
src/          # application code
tests/
requirements.txt
.env.example
README.md
.claude/
```
- Use `logging`, not `print()`
- Explicit error handling
- `pytest` + `ruff` configured

## Flutter/Dart Bootstrap
```
lib/
  core/       # shared widgets, models, services
  features/   # feature modules
  app/        # router, shell
test/
pubspec.yaml
analysis_options.yaml
CLAUDE.md
.claude/
```
- `flutter analyze` → 0 issues always
- `flutter test` → all passing always
- Riverpod for state, GoRouter for navigation

## C# Bootstrap
```
SolutionName.sln
SrcProject/
TestProject/
README.md
.claude/
```
- `dotnet build` → 0 errors always
- `dotnet test` → all passing
- No `Console.WriteLine` in production code

## Default Claude Behavior in New Projects
1. Check for `.claude/`
2. If missing, create the Hawk Informatica Pro default package
3. Load `AGENT_RULES.md`
4. Follow incremental workflow strictly

## Handoff Standard (OBRIGATÓRIO em todo repositório — regra do usuário, 2026-11-09)
Todo projeto deve poder ser continuado por QUALQUER IA. Instale com:
`pwsh ~/.claude/scripts/new-project-handoff.ps1 -Path . -Name "<nome>" -Description "<...>" -Stack "<...>" -Commands "<...>" -Map "<...>"`
- Cria `AGENTS.md` (regras para qualquer IA), `CLAUDE.md`/`GEMINI.md`/`.github/copilot-instructions.md`/`.cursorrules` (ponteiros),
  `docs/STATUS.md` (estado/pendências/decisões/log), `docs/ARCHITECTURE.md`, `scripts/check-handoff.ps1`, `.githooks/pre-commit`,
  `.github/workflows/handoff-check.yml`.
- Ativa o hook: `git config core.hooksPath .githooks` (o script faz isso se já for um repo git).
- Modelos em `~/.claude/templates/handoff/`. Nunca sobrescreve arquivo existente (`-Force` para substituir).
- Todo item do "New Repository Checklist" acima inclui: [ ] Handoff Standard instalado e preenchido.
