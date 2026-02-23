param(
  [string]$BaseUrl = "http://localhost:5003"
)

$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true

function Wait-Health([string]$url) {
  for ($i = 0; $i -lt 120; $i++) {
    Start-Sleep -Milliseconds 500
    try {
      $h = Invoke-RestMethod -Uri "$url/health" -Method Get -TimeoutSec 2
      if ($h.status -eq "ok") { return $true }
    }
    catch {}
  }
  return $false
}

function Assert($condition, $message) {
  if (-not $condition) { throw $message }
}

function EnvFlag([string]$name, [bool]$defaultValue = $true) {
  $raw = [Environment]::GetEnvironmentVariable($name)
  if ([string]::IsNullOrWhiteSpace($raw)) { return $defaultValue }
  return -not ($raw.Trim().ToLowerInvariant() -in @("0", "false", "no", "off"))
}

function EnvInt([string]$name, [int]$defaultValue) {
  $raw = [Environment]::GetEnvironmentVariable($name)
  if ([string]::IsNullOrWhiteSpace($raw)) { return $defaultValue }
  $parsed = 0
  if ([int]::TryParse($raw, [ref]$parsed)) { return $parsed }
  return $defaultValue
}

$originalAspNetEnv = $env:ASPNETCORE_ENVIRONMENT
$originalDatabaseProvider = $env:DatabaseProvider
$originalDefaultConnection = $env:ConnectionStrings__DefaultConnection
$originalUseSqliteForDev = $env:UseSqliteForDev
if ([string]::IsNullOrWhiteSpace($env:ASPNETCORE_ENVIRONMENT)) {
  $env:ASPNETCORE_ENVIRONMENT = "Development"
}
if ([string]::IsNullOrWhiteSpace($env:DatabaseProvider)) {
  $env:DatabaseProvider = "sqlserver"
}
if ([string]::IsNullOrWhiteSpace($env:ConnectionStrings__DefaultConnection)) {
  $env:ConnectionStrings__DefaultConnection = "Server=localhost,1433;Database=BusinessDirectory;User Id=sa;Password=Your_strong_password123!;Encrypt=False;TrustServerCertificate=True"
}
$env:UseSqliteForDev = "false"

dotnet build BusinessDirectory.sln | Out-Null
$api = Start-Process dotnet -ArgumentList "run --no-build --project BusinessDirectory/BusinessDirectory.API.csproj --urls $BaseUrl" -PassThru
try {
  Assert (Wait-Health $BaseUrl) "API nuk u ngrit ne kohe."

  $email = "ci." + [guid]::NewGuid().ToString("N") + "@test.local"
  $password = "Pass12345!"

  # Register should work, but login should fail before email verification.
  Invoke-RestMethod -Uri "$BaseUrl/api/auth/register" -Method Post -ContentType "application/json" -Body (@{
    username = "ci.user"
    email = $email
    password = $password
    role = 0
  } | ConvertTo-Json) | Out-Null

  $got401 = $false
  try {
    Invoke-RestMethod -Uri "$BaseUrl/api/auth/login" -Method Post -ContentType "application/json" -Body (@{
      email = $email
      password = $password
    } | ConvertTo-Json) -ErrorAction Stop | Out-Null
  }
  catch {
    $status = $_.Exception.Response.StatusCode.value__
    if ($status -eq 401) { $got401 = $true }
  }
  Assert $got401 "Login para verifikimit duhet te jape 401."

  # Contract test with Dredd (optional in CI)
  if (EnvFlag "RUN_CONTRACT_TESTS" $true) {
    & "$PSScriptRoot/contract_test_dredd.ps1"
  } else {
    Write-Host "Skipping Dredd contract tests (RUN_CONTRACT_TESTS=false)."
  }

  # Security tests
  if (EnvFlag "RUN_SECURITY_TESTS" $true) {
    $security = & "$PSScriptRoot/security_hardening_tests.ps1" -BaseUrl $BaseUrl | ConvertFrom-Json
    Assert ($security.summary.failed -eq 0) "Security hardening tests deshtuan."
  } else {
    Write-Host "Skipping security tests (RUN_SECURITY_TESTS=false)."
  }

  # Load tests (tuned for shared CI runners)
  if (EnvFlag "RUN_LOAD_TESTS" $true) {
    $loadConcurrency = [Math]::Max(1, (EnvInt "LOAD_TEST_CONCURRENCY" 10))
    $loadTotalRequests = [Math]::Max($loadConcurrency, (EnvInt "LOAD_TEST_TOTAL_REQUESTS" 120))
    $load = & "$PSScriptRoot/load_test_search.ps1" -BaseUrl $BaseUrl -Concurrency $loadConcurrency -TotalRequests $loadTotalRequests | ConvertFrom-Json
    Write-Host "Load metrics: errorRate=$($load.errorRatePercent)% p95=$($load.p95Ms)ms p99=$($load.p99Ms)ms sent=$($load.sent)"
    Assert ($load.errorRatePercent -lt 10) "Load test error rate shume i larte: $($load.errorRatePercent)%"
    Assert ($load.p95Ms -lt 2500) "Load test p95 eshte shume i larte: $($load.p95Ms)ms"
  } else {
    Write-Host "Skipping load tests (RUN_LOAD_TESTS=false)."
  }

  # Aggressive k6 profile
  if (EnvFlag "RUN_K6_TESTS" $false) {
    $k6 = & "$PSScriptRoot/load_test_k6.ps1" -BaseUrl $BaseUrl | ConvertFrom-Json
    Write-Host "k6 metrics: failedRate=$($k6.httpReqFailedRate) p95=$($k6.p95Ms)ms p99=$($k6.p99Ms)ms"
    Assert ($k6.httpReqFailedRate -lt 0.1) "k6 failed rate shume e larte: $($k6.httpReqFailedRate)"
    Assert ($k6.p95Ms -lt 2500) "k6 p95 shume i larte: $($k6.p95Ms)ms"
  } else {
    Write-Host "Skipping k6 tests (RUN_K6_TESTS=false)."
  }
}
finally {
  if ($api -and -not $api.HasExited) {
    Stop-Process -Id $api.Id -Force
  }
  if ([string]::IsNullOrWhiteSpace($originalAspNetEnv)) {
    Remove-Item Env:ASPNETCORE_ENVIRONMENT -ErrorAction SilentlyContinue
  } else {
    $env:ASPNETCORE_ENVIRONMENT = $originalAspNetEnv
  }

  if ([string]::IsNullOrWhiteSpace($originalDatabaseProvider)) {
    Remove-Item Env:DatabaseProvider -ErrorAction SilentlyContinue
  } else {
    $env:DatabaseProvider = $originalDatabaseProvider
  }

  if ([string]::IsNullOrWhiteSpace($originalDefaultConnection)) {
    Remove-Item Env:ConnectionStrings__DefaultConnection -ErrorAction SilentlyContinue
  } else {
    $env:ConnectionStrings__DefaultConnection = $originalDefaultConnection
  }

  if ([string]::IsNullOrWhiteSpace($originalUseSqliteForDev)) {
    Remove-Item Env:UseSqliteForDev -ErrorAction SilentlyContinue
  } else {
    $env:UseSqliteForDev = $originalUseSqliteForDev
  }
}

# Backup/restore test (done with API stopped)
if (EnvFlag "RUN_BACKUP_RESTORE_TEST" $false) {
  $backup = & "$PSScriptRoot/sqlserver_backup_restore_test.ps1" | ConvertFrom-Json
  Assert $backup.pass "Backup/restore test deshtoi."
} else {
  Write-Host "Skipping backup/restore test (RUN_BACKUP_RESTORE_TEST=false)."
}

Write-Host "CI backend suite passed."
