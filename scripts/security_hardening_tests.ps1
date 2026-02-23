param(
  [string]$BaseUrl = "http://localhost:5003"
)

$ErrorActionPreference = "Stop"

function New-Result($name, $ok, $detail) {
  [pscustomobject]@{ test = $name; pass = [bool]$ok; detail = $detail }
}

function Get-Status($err) {
  try { return $err.Exception.Response.StatusCode.value__ } catch { return -1 }
}

function To-Base64Url([byte[]]$bytes) {
  [Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
}

function New-SignedJwt([string]$secret, [string]$userId, [string]$email, [string]$role, [long]$expUnix, [long]$iatUnix) {
  $headerJson = '{"alg":"HS256","typ":"JWT"}'
  $payloadJson = '{"sub":"' + $userId + '","email":"' + $email + '","role":"' + $role + '","iat":' + $iatUnix + ',"exp":' + $expUnix + ',"iss":"BusinessDirectory","aud":"BusinessDirectory"}'
  $header = To-Base64Url([Text.Encoding]::UTF8.GetBytes($headerJson))
  $payload = To-Base64Url([Text.Encoding]::UTF8.GetBytes($payloadJson))
  $unsigned = "$header.$payload"

  $hmac = [System.Security.Cryptography.HMACSHA256]::new([Text.Encoding]::UTF8.GetBytes($secret))
  $sigBytes = $hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes($unsigned))
  $sig = To-Base64Url($sigBytes)
  return "$unsigned.$sig"
}

$results = New-Object System.Collections.Generic.List[object]

$adminLogin = Invoke-RestMethod -Uri "$BaseUrl/api/auth/login" -Method Post -ContentType "application/json" -Body (@{
  email = "admin@business.local"
  password = "Admin12345!"
} | ConvertTo-Json)

$token = $adminLogin.token
$results.Add((New-Result "admin_login_token" (-not [string]::IsNullOrWhiteSpace($token)) "token issued"))

# Tampered JWT should fail
$tampered = $token.Substring(0, $token.Length - 1) + "x"
try {
  Invoke-RestMethod -Uri "$BaseUrl/api/admin/dashboard" -Headers @{ Authorization = "Bearer $tampered" } -Method Get -ErrorAction Stop | Out-Null
  $results.Add((New-Result "tampered_jwt_401" $false "expected 401"))
}
catch {
  $status = Get-Status $_
  $results.Add((New-Result "tampered_jwt_401" ($status -eq 401) "status=$status"))
}

$secret = "your-super-secret-key-min-32-characters-long-for-hs256"
$now = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()

# Expired token outside clock skew should fail (401)
$expiredFarToken = New-SignedJwt -secret $secret -userId "11111111-1111-1111-1111-111111111111" -email "expired-far@test.local" -role "Admin" -iatUnix ($now - 3600) -expUnix ($now - 90)
try {
  Invoke-RestMethod -Uri "$BaseUrl/api/admin/dashboard" -Headers @{ Authorization = "Bearer $expiredFarToken" } -Method Get -ErrorAction Stop | Out-Null
  $results.Add((New-Result "expired_far_clockskew_401" $false "expected 401"))
}
catch {
  $status = Get-Status $_
  $results.Add((New-Result "expired_far_clockskew_401" ($status -eq 401) "status=$status"))
}

# Token slightly expired but inside configured clock skew should still be accepted
$expiredNearToken = New-SignedJwt -secret $secret -userId "11111111-1111-1111-1111-111111111111" -email "expired-near@test.local" -role "Admin" -iatUnix ($now - 3600) -expUnix ($now - 10)
try {
  Invoke-RestMethod -Uri "$BaseUrl/api/admin/dashboard" -Headers @{ Authorization = "Bearer $expiredNearToken" } -Method Get -ErrorAction Stop | Out-Null
  $results.Add((New-Result "expired_near_clockskew_accepted" $true "status=200"))
}
catch {
  $status = Get-Status $_
  $results.Add((New-Result "expired_near_clockskew_accepted" $false "status=$status"))
}

# Brute-force matrix by IP+email combinations
$matrixEmails = @(
  ("matrix." + [guid]::NewGuid().ToString("N") + "@test.local"),
  ("matrix." + [guid]::NewGuid().ToString("N") + "@test.local"),
  ("matrix." + [guid]::NewGuid().ToString("N") + "@test.local")
)

$matrixHits = @()
foreach ($email in $matrixEmails) {
  $hit429 = $false
  for ($i = 0; $i -lt 12; $i++) {
    try {
      Invoke-RestMethod -Uri "$BaseUrl/api/auth/login" -Method Post -ContentType "application/json" -Body (@{
        email = $email
        password = "WrongPassword!"
      } | ConvertTo-Json) -ErrorAction Stop | Out-Null
    }
    catch {
      if ((Get-Status $_) -eq 429) {
        $hit429 = $true
        break
      }
    }
  }
  $matrixHits += $hit429
}

$allMatrixLimited = ($matrixHits | Where-Object { $_ -eq $true }).Count -eq $matrixEmails.Count
$results.Add((New-Result "bruteforce_ip_email_matrix_429" $allMatrixLimited "hits=$($matrixHits -join ',')"))

$passed = ($results | Where-Object { $_.pass }).Count
$failed = ($results | Where-Object { -not $_.pass }).Count
[pscustomobject]@{
  summary = [pscustomobject]@{ total = $results.Count; passed = $passed; failed = $failed }
  results = $results
} | ConvertTo-Json -Depth 6
