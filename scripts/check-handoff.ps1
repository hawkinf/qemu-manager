# Verifica a regra de handoff: mudança de código exige atualização de docs/STATUS.md no MESMO conjunto de mudanças.
# Uso local (pré-commit):   pwsh scripts/check-handoff.ps1
# Uso em CI (intervalo):    pwsh scripts/check-handoff.ps1 -Range "<base>...<head>"
# Escape (só mudança trivial: typo/formatação): defina HANDOFF_SKIP=1 e diga isso na mensagem do commit.
param([string]$Range = '')

$ErrorActionPreference = 'Stop'
$root = (& git rev-parse --show-toplevel).Trim()
Set-Location $root

if ($env:HANDOFF_SKIP -eq '1') { Write-Host '[handoff] HANDOFF_SKIP=1: verificação ignorada (declare o motivo no commit).'; exit 0 }

$status = Join-Path $root 'docs/STATUS.md'
if (-not (Test-Path $status)) {
    Write-Host '[handoff] ERRO: docs/STATUS.md não existe. Crie-o (modelo em ~/.claude/templates/handoff) — outra IA precisa dele para continuar o projeto.'
    exit 1
}

$files = if ($Range) { & git diff --name-only $Range } else { & git diff --cached --name-only }
$files = @($files | Where-Object { $_ -and $_.Trim() })
if ($files.Count -eq 0) { exit 0 }

# Arquivos que NÃO exigem atualização do STATUS (documentação e metadados).
$exempt = '^(docs/|\.github/(ISSUE_TEMPLATE|PULL_REQUEST_TEMPLATE)|LICENSE|\.gitignore|\.gitattributes)|\.md$'
$needsStatus = @($files | Where-Object { $_ -notmatch $exempt })
if ($needsStatus.Count -eq 0) { exit 0 }

if ($files -notcontains 'docs/STATUS.md') {
    Write-Host ''
    Write-Host '[handoff] BLOQUEADO: há mudança de código/configuração, mas docs/STATUS.md não foi atualizado.'
    Write-Host '          Toda IA é obrigada a atualizar o STATUS (estado atual, pendências e uma linha no Log) no mesmo commit.'
    Write-Host '          Arquivos que exigem atualização:'
    $needsStatus | Select-Object -First 10 | ForEach-Object { Write-Host "            - $_" }
    Write-Host '          Só mudança trivial (typo/formatação): HANDOFF_SKIP=1 git commit ... e explique na mensagem.'
    exit 1
}

# O STATUS foi tocado: confere que ainda tem as seções essenciais (evita "atualizar" apagando o conteúdo).
$text = Get-Content $status -Raw
foreach ($needle in @('Log', 'Pendências', 'Última atualização')) {
    if ($text -notmatch [regex]::Escape($needle)) {
        Write-Host "[handoff] BLOQUEADO: docs/STATUS.md perdeu a seção/campo '$needle'. Restaure a estrutura (veja o modelo)."
        exit 1
    }
}
exit 0
