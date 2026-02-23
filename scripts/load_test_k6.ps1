param(
  [string]$BaseUrl = "http://127.0.0.1:5003",
  [string]$ScriptPath = "tests/load/k6-search.js"
)

$ErrorActionPreference = "Stop"

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
  throw "Docker nuk u gjet. k6 runner perdor docker image grafana/k6."
}

$scriptFullPath = Resolve-Path $ScriptPath
$scriptDir = Split-Path $scriptFullPath -Parent
$scriptName = Split-Path $scriptFullPath -Leaf
$summaryPath = Join-Path $scriptDir "k6-summary.json"
if (Test-Path $summaryPath) {
  Remove-Item $summaryPath -Force
}

$mount = "$($scriptDir):/scripts"
$dockerBaseUrl = $BaseUrl
$cmd = @("run", "--rm")
$isWindowsHost = $false
if ($env:OS -eq "Windows_NT") {
  $isWindowsHost = $true
} elseif (Get-Variable -Name IsWindows -ErrorAction SilentlyContinue) {
  $isWindowsHost = [bool]$IsWindows
}

if ($isWindowsHost) {
  $dockerBaseUrl = $dockerBaseUrl.Replace("127.0.0.1", "host.docker.internal").Replace("localhost", "host.docker.internal")
} else {
  $cmd += @("--network", "host")
}

$cmd += @(
  "-e", "BASE_URL=$dockerBaseUrl",
  "-v", $mount,
  "grafana/k6",
  "run", "/scripts/$scriptName",
  "--summary-export", "/scripts/k6-summary.json"
)

& docker @cmd 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) {
  throw "k6 load test deshtoi me exit code $LASTEXITCODE."
}

if (-not (Test-Path $summaryPath)) {
  throw "k6 summary file nuk u gjenerua."
}

$summary = Get-Content $summaryPath -Raw | ConvertFrom-Json
$failedMetric = $summary.metrics.http_req_failed
$durationMetric = $summary.metrics.http_req_duration

$failedRate =
  if ($failedMetric.PSObject.Properties["values"]) { [double]$failedMetric.values.rate }
  elseif ($failedMetric.PSObject.Properties["value"]) { [double]$failedMetric.value }
  else { 1.0 }

$durationValues =
  if ($durationMetric.PSObject.Properties["values"]) { $durationMetric.values }
  else { $durationMetric }

$p95Prop = $durationValues.PSObject.Properties["p(95)"]
$p99Prop = $durationValues.PSObject.Properties["p(99)"]
$p95 = if ($p95Prop) { [double]$p95Prop.Value } else { [double]$durationValues.p95 }
$p99 = if ($p99Prop) { [double]$p99Prop.Value } else { [double]$durationValues.p99 }

[pscustomobject]@{
  httpReqFailedRate = [math]::Round($failedRate, 4)
  p95Ms = [math]::Round($p95, 2)
  p99Ms = [math]::Round($p99, 2)
} | ConvertTo-Json
