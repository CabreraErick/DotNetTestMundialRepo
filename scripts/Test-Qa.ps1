# Full reproducible QA including real SQL Server integration tests and HTTP smoke tests.
[CmdletBinding()]
param([switch]$SkipStart)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$previousConnection = $env:QA_SQLSERVER_CONNECTION
function Assert-QaExit([string]$step) {
    if ($LASTEXITCODE -ne 0) { throw "QA failed: $step (exit $LASTEXITCODE)." }
}
Push-Location $repoRoot
try {
    if (-not $SkipStart) { & (Join-Path $PSScriptRoot 'Start-Qa.ps1') }
    if (-not (Test-Path -LiteralPath '.env')) { throw 'Run scripts/Start-Qa.ps1 to prepare .env.' }
    $settings = @{}
    foreach ($line in Get-Content -LiteralPath '.env') {
        if ($line -match '^\s*([^#=\s]+)\s*=(.*)$') {
            $settings[$Matches[1]] = $Matches[2].Trim().Trim('"').Trim("'")
        }
    }
    $sqlPort = if ($settings.SQLSERVER_HOST_PORT) { $settings.SQLSERVER_HOST_PORT } else { '14330' }
    $apiPort = if ($settings.API_HOST_PORT) { $settings.API_HOST_PORT } else { '5164' }
    $frontendPort = if ($settings.FRONTEND_HOST_PORT) { $settings.FRONTEND_HOST_PORT } else { '3000' }
    if (-not $settings.MSSQL_SA_PASSWORD) { throw 'MSSQL_SA_PASSWORD is required in .env.' }
    $quotedPassword = $settings.MSSQL_SA_PASSWORD.Replace('"', '""')
    $env:QA_SQLSERVER_CONNECTION = "Server=localhost,$sqlPort;Database=master;User Id=sa;Password=`"$quotedPassword`";Encrypt=True;TrustServerCertificate=True"
    $health = Invoke-WebRequest -UseBasicParsing "http://localhost:$apiPort/health"
    $front = Invoke-WebRequest -UseBasicParsing "http://localhost:$frontendPort"
    if ($health.StatusCode -ne 200 -or $front.StatusCode -ne 200) { throw 'HTTP health checks failed.' }
    dotnet restore DotNetTestMundial.sln
    Assert-QaExit 'restore'
    dotnet format DotNetTestMundial.sln --verify-no-changes --no-restore
    Assert-QaExit 'format'
    $resultsPath = Join-Path $repoRoot ('TestResults/qa-' + [Guid]::NewGuid().ToString('N'))
    dotnet test DotNetTestMundial.sln --configuration Release --no-restore --logger trx --results-directory $resultsPath
    Assert-QaExit '.NET tests (including SQL Server)'
    $reports = @(Get-ChildItem -LiteralPath $resultsPath -Filter '*.trx')
    if ($reports.Count -ne 4) { throw 'Expected one TRX report from each of the four test projects.' }
    foreach ($report in $reports) {
        [xml]$result = Get-Content -Raw -LiteralPath $report.FullName
        $counts = $result.SelectSingleNode("//*[local-name()='Counters']")
        if ($null -eq $counts -or [int]$counts.total -le 0 -or
            [int]$counts.passed -ne [int]$counts.total) {
            throw "Tests failed, were skipped, or could not be discovered: $($report.Name)."
        }
    }
    pnpm --dir frontend install --frozen-lockfile
    Assert-QaExit 'frontend install'
    pnpm --dir frontend lint
    Assert-QaExit 'frontend lint'
    pnpm --dir frontend build
    Assert-QaExit 'frontend build'
    npx --yes newman run postman/DotNetTestMundial.postman_collection.json --environment postman/DotNetTestMundial.environment.json --env-var "baseUrl=http://localhost:$apiPort" --reporters cli
    Assert-QaExit 'HTTP smoke tests'
    git diff --check
    Assert-QaExit 'diff integrity'
    Write-Host 'QA completed successfully. SQL fixture databases were cleaned up; the app stack remains running.'
} finally {
    $env:QA_SQLSERVER_CONNECTION = $previousConnection
    Pop-Location
}
