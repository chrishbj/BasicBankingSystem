param(
    [string]$CustomerBaseUrl = "http://localhost:5101",
    [string]$AccountBaseUrl = "http://localhost:5102",
    [string]$DepositBaseUrl = "http://localhost:5103",
    [string]$AuditBaseUrl = "http://localhost:5104",
    [string]$ApiKey = "local-dev-api-key",
    [decimal]$DepositAmount = 1000,
    [string]$Currency = "CNY",
    [int]$PollAttempts = 30,
    [int]$PollDelaySeconds = 2
)

$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Invoke-JsonRequest {
    param(
        [Parameter(Mandatory = $true)][string]$Method,
        [Parameter(Mandatory = $true)][string]$Uri,
        [object]$Body,
        [hashtable]$Headers
    )

    $params = @{
        Method      = $Method
        Uri         = $Uri
        ContentType = "application/json"
    }

    if ($null -ne $Headers) {
        $params.Headers = $Headers
    }

    if ($null -ne $Body) {
        $params.Body = ($Body | ConvertTo-Json -Depth 10)
    }

    return Invoke-RestMethod @params
}

function Assert-ServiceHealthy {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$BaseUrl
    )

    $health = Invoke-WebRequest -Uri "$BaseUrl/api/v1/health" -UseBasicParsing
    if ($health.StatusCode -ne 200) {
        throw "$Name health check failed with status code $($health.StatusCode)."
    }

    Write-Host "$Name healthy at $BaseUrl" -ForegroundColor Green
}

function Wait-ForDepositCompletion {
    param(
        [Parameter(Mandatory = $true)][string]$TransactionId,
        [Parameter(Mandatory = $true)][string]$BaseUrl,
        [Parameter(Mandatory = $true)][hashtable]$Headers,
        [Parameter(Mandatory = $true)][int]$Attempts,
        [Parameter(Mandatory = $true)][int]$DelaySeconds
    )

    for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
        $deposit = Invoke-RestMethod -Method Get -Uri "$BaseUrl/api/v1/deposits/$TransactionId" -Headers $Headers
        Write-Host ("Attempt {0}/{1}: deposit status = {2}" -f $attempt, $Attempts, $deposit.status)

        if ($deposit.status -eq 3 -or $deposit.status -eq 5) {
            return $deposit
        }

        Start-Sleep -Seconds $DelaySeconds
    }

    throw "Deposit $TransactionId did not complete within the expected time window."
}

function Wait-ForAuditRecord {
    param(
        [Parameter(Mandatory = $true)][string]$CorrelationId,
        [Parameter(Mandatory = $true)][string]$BaseUrl,
        [Parameter(Mandatory = $true)][hashtable]$Headers,
        [Parameter(Mandatory = $true)][int]$Attempts,
        [Parameter(Mandatory = $true)][int]$DelaySeconds
    )

    for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
        $response = Invoke-RestMethod -Method Get -Uri "$BaseUrl/api/v1/audits?pageNumber=1&pageSize=100" -Headers $Headers
        $matched = @($response.items | Where-Object { $_.correlationId -eq $CorrelationId })

        if ($matched.Count -gt 0) {
            return ,$matched
        }

        Start-Sleep -Seconds $DelaySeconds
    }

    throw "No audit record was found for correlation id $CorrelationId."
}

$uniqueSuffix = Get-Date -Format "yyyyMMddHHmmss"
$identityNumber = "MTC-$uniqueSuffix"
$mobileSuffix = Get-Random -Minimum 10000000 -Maximum 99999999
$mobile = "138$mobileSuffix"
$idempotencyKey = "manual-deposit-$uniqueSuffix"
$correlationId = "manual-correlation-$uniqueSuffix"
$externalHeaders = @{
    "X-Api-Key" = $ApiKey
}

Write-Step "Checking service health"
Assert-ServiceHealthy -Name "Customer" -BaseUrl $CustomerBaseUrl
Assert-ServiceHealthy -Name "Account" -BaseUrl $AccountBaseUrl
Assert-ServiceHealthy -Name "Deposit" -BaseUrl $DepositBaseUrl
Assert-ServiceHealthy -Name "Audit" -BaseUrl $AuditBaseUrl

Write-Step "Creating a customer"
$customer = Invoke-JsonRequest -Method Post -Uri "$CustomerBaseUrl/api/v1/customers" -Body @{
    fullName       = "Manual Test Customer $uniqueSuffix"
    identityType   = "NationalId"
    identityNumber = $identityNumber
    mobile         = $mobile
    email          = "manual.$uniqueSuffix@example.com"
    address        = @{
        country    = "CN"
        province   = "Beijing"
        city       = "Beijing"
        line1      = "No. 1 Banking Road"
        postalCode = "100000"
    }
    riskLevel      = "Low"
} -Headers $externalHeaders
Write-Host "CustomerId: $($customer.customerId)"

Write-Step "Activating the customer"
$activatedCustomer = Invoke-JsonRequest -Method Post -Uri "$CustomerBaseUrl/api/v1/customers/$($customer.customerId)/status" -Body @{
    targetStatus = 2
    reason       = "Manual smoke test activation"
} -Headers $externalHeaders
Write-Host "Customer status: $($activatedCustomer.status)"

Write-Step "Opening an account"
$account = Invoke-JsonRequest -Method Post -Uri "$AccountBaseUrl/api/v1/accounts" -Body @{
    customerId  = $customer.customerId
    accountType = "Checking"
    currency    = $Currency
} -Headers $externalHeaders
Write-Host "AccountId: $($account.accountId)"

Write-Step "Submitting a deposit"
$deposit = Invoke-JsonRequest -Method Post -Uri "$DepositBaseUrl/api/v1/deposits" -Body @{
    customerId      = $customer.customerId
    accountId       = $account.accountId
    amount          = $DepositAmount
    currency        = $Currency
    channel         = 1
    referenceNumber = "MANUAL-REF-$uniqueSuffix"
    note            = "Manual smoke test"
} -Headers @{
    "Idempotency-Key" = $idempotencyKey
    "X-Correlation-Id" = $correlationId
    "X-Api-Key" = $ApiKey
}
Write-Host "TransactionId: $($deposit.transactionId)"
Write-Host "Initial deposit status: $($deposit.status)"

Write-Step "Waiting for deposit completion"
$completedDeposit = Wait-ForDepositCompletion -TransactionId $deposit.transactionId -BaseUrl $DepositBaseUrl -Headers $externalHeaders -Attempts $PollAttempts -DelaySeconds $PollDelaySeconds
Write-Host "Final deposit status: $($completedDeposit.status)"

if ($completedDeposit.status -ne 3) {
    throw "Deposit did not succeed. FailureCode=$($completedDeposit.failureCode), FailureReason=$($completedDeposit.failureReason)"
}

Write-Step "Verifying account balance"
$updatedAccount = Invoke-RestMethod -Method Get -Uri "$AccountBaseUrl/api/v1/accounts/$($account.accountId)" -Headers $externalHeaders
Write-Host "Available balance: $($updatedAccount.availableBalance)"
Write-Host "Ledger balance: $($updatedAccount.ledgerBalance)"

if ([decimal]$updatedAccount.availableBalance -ne $DepositAmount) {
    throw "Available balance mismatch. Expected $DepositAmount but got $($updatedAccount.availableBalance)."
}

if ([decimal]$updatedAccount.ledgerBalance -ne $DepositAmount) {
    throw "Ledger balance mismatch. Expected $DepositAmount but got $($updatedAccount.ledgerBalance)."
}

Write-Step "Verifying audit records"
$auditRecords = Wait-ForAuditRecord -CorrelationId $correlationId -BaseUrl $AuditBaseUrl -Headers $externalHeaders -Attempts $PollAttempts -DelaySeconds $PollDelaySeconds
Write-Host "Audit records found: $($auditRecords.Count)"

$summary = [PSCustomObject]@{
    CustomerId       = $customer.customerId
    AccountId        = $account.accountId
    TransactionId    = $completedDeposit.transactionId
    DepositStatus    = $completedDeposit.status
    AvailableBalance = $updatedAccount.availableBalance
    LedgerBalance    = $updatedAccount.ledgerBalance
    CorrelationId    = $correlationId
    AuditRecords     = $auditRecords.Count
}

Write-Step "Smoke test completed successfully"
$summary | Format-List
