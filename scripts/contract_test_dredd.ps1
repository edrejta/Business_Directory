param(
  [string]$ApiDescription = "docs/openapi.yaml",
  [string]$BaseUrl = "http://127.0.0.1:5003",
  [string]$HooksFile = "tests/contract/hooks.js"
)

$ErrorActionPreference = "Stop"

$dreddCmd = Get-Command dredd -ErrorAction SilentlyContinue
if (-not $dreddCmd) {
  throw "Dredd nuk eshte i instaluar. Instalo me: npm i -g dredd"
}

dredd $ApiDescription $BaseUrl --hookfiles $HooksFile --language nodejs --color
if ($LASTEXITCODE -ne 0) {
  throw "Dredd contract tests deshtuan me exit code $LASTEXITCODE."
}
