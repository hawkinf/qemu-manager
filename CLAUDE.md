# CLAUDE.md — QEMU Manager

As regras deste projeto valem para QUALQUER IA e estão em **`AGENTS.md`** (leia primeiro, é obrigatório).
Estado atual, pendências e log: **`docs/STATUS.md`**. Mapa do código: **`docs/ARCHITECTURE.md`**.

Resumo das obrigações (detalhe em `AGENTS.md`):
1. Antes de trabalhar: ler `AGENTS.md`, `docs/STATUS.md`, `docs/ARCHITECTURE.md`, `git log`; resumir ao usuário o que entendeu.
2. Depois de mudar algo: atualizar `docs/STATUS.md` (estado, pendências, linha no Log) no MESMO commit.
3. Regras globais do usuário (`~/.claude/CLAUDE.md`) continuam valendo; em conflito, vale o mais específico deste projeto.

Stack: C# / .NET 8 WinForms; Python 3 + Tkinter (referência); PowerShell

Comandos:
cd programa; dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish  (exe em programa/publish/QemuManager.exe). QEMU esperado em C:\Program Files\qemu\
