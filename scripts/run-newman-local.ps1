param(
    [string]$CollectionPath = "postman/BasicBankingSystem-Local.postman_collection.json",
    [string]$EnvironmentPath = "postman/BasicBankingSystem-Local-Docker.postman_environment.json",
    [switch]$EnsureDockerStack,
    [string]$ApiKey = "local-dev-api-key",
    [int]$PollMaxAttempts = 20
)

$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

if ($EnsureDockerStack) {
    Write-Step "Starting Docker Desktop stack"
    docker compose --env-file infra/.env.example -f infra/docker-compose.docker-desktop.yml up --build -d
}

Write-Step "Running Newman collection"
$env:NEWMAN_POLL_MAX_ATTEMPTS = "$PollMaxAttempts"

npx --yes newman run $CollectionPath `
  -e $EnvironmentPath `
  --env-var "apiKey=$ApiKey" `
  --env-var "pollMaxAttempts=$PollMaxAttempts"
