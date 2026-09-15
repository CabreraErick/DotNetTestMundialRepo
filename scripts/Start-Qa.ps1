# Bootstrap a local-only environment, then start standard Docker Compose.
[CmdletBinding()]
param([switch]$NoBuild)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot
try {
    if (-not (Test-Path -LiteralPath '.env')) {
        if (Test-Path -LiteralPath '.env.docker') {
            Copy-Item -LiteralPath '.env.docker' -Destination '.env'
            Write-Host 'Reusing existing local configuration in .env (ignored by Git).'
        } else {
            $password = 'Qa1!' + [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(24))
            $template = Get-Content -Raw -LiteralPath '.env.docker.example'
            $template = $template.Replace('REPLACE_WITH_A_STRONG_PASSWORD', $password)
            [IO.File]::WriteAllText((Join-Path $repoRoot '.env'), $template, [Text.UTF8Encoding]::new($false))
            Write-Host 'Generated local configuration in .env. Do not publish this file.'
        }
    }
    docker compose config --quiet
    if ($LASTEXITCODE -ne 0) { throw 'Docker Compose configuration is invalid.' }
    $composeArguments = @('compose', 'up', '--detach', '--wait', '--wait-timeout', '180')
    if (-not $NoBuild) { $composeArguments += '--build' }
    & docker @composeArguments
    if ($LASTEXITCODE -ne 0) { throw 'The QA stack did not become healthy.' }
    docker compose ps
    if ($LASTEXITCODE -ne 0) { throw 'Cannot inspect QA containers.' }
} finally { Pop-Location }
