param(
  [string]$BaseUrl = "http://localhost:5003",
  [int]$Concurrency = 20,
  [int]$TotalRequests = 300
)

$ErrorActionPreference = "Stop"

if ($TotalRequests -lt $Concurrency) {
  $TotalRequests = $Concurrency
}

$scriptBlock = {
  param($url, $count)
  $times = @()
  $errors = 0
  for ($i = 0; $i -lt $count; $i++) {
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    try {
      Invoke-RestMethod -Uri $url -Method Get -TimeoutSec 10 | Out-Null
    }
    catch {
      $errors++
    }
    $sw.Stop()
    $times += [int]$sw.ElapsedMilliseconds
  }
  [pscustomobject]@{ times = $times; errors = $errors }
}

$url = "$BaseUrl/search?onlyWithCoordinates=true&lat=42.66&lng=21.16&radiusKm=20&limit=20&page=1"
$perWorker = [Math]::Ceiling($TotalRequests / $Concurrency)
$jobs = @()
for ($i = 0; $i -lt $Concurrency; $i++) {
  $jobs += Start-Job -ScriptBlock $scriptBlock -ArgumentList $url, $perWorker
}

Wait-Job $jobs | Out-Null
$results = $jobs | ForEach-Object { Receive-Job $_ }
$jobs | Remove-Job

$allTimes = @()
$totalErrors = 0
foreach ($r in $results) {
  $allTimes += $r.times
  $totalErrors += $r.errors
}

$allTimes = $allTimes | Sort-Object
$sent = $Concurrency * $perWorker
$ok = $sent - $totalErrors
$p50 = $allTimes[[Math]::Floor($allTimes.Count * 0.50)]
$p95 = $allTimes[[Math]::Ceiling($allTimes.Count * 0.95) - 1]
$p99 = $allTimes[[Math]::Ceiling($allTimes.Count * 0.99) - 1]
$errorRate = if ($sent -eq 0) { 1 } else { [Math]::Round(($totalErrors / $sent) * 100, 2) }

[pscustomobject]@{
  sent = $sent
  ok = $ok
  errors = $totalErrors
  errorRatePercent = $errorRate
  p50Ms = $p50
  p95Ms = $p95
  p99Ms = $p99
} | ConvertTo-Json
