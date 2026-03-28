# Phase 1 User Stories and Acceptance Criteria

## Epics

- Epic A: Customer Management
- Epic B: Account Management
- Epic C: Deposit Processing
- Epic D: Transaction Query
- Epic E: Audit and Compliance
- Epic F: Platform Foundations

## Customer

### US-A01 Create Customer

As a teller or operations user, I want to create a customer profile so the customer can open accounts and use banking services.

Acceptance criteria:

- Valid request returns `CustomerId` and `CustomerNumber`
- Identity document and mobile are unique
- Audit event is created

### US-A02 Get Customer Details

- Lookup by `CustomerId`
- Return profile, contact data, status, and timestamps

### US-A03 Search Customers

- Filter by name, mobile, identity number, customer number, status
- Paginated result

### US-A04 Update Contact Details

- Update mobile, email, address
- Reject if mobile belongs to another customer
- Write audit log

### US-A05 Change Customer Status

- Support `Pending`, `Active`, `Frozen`, `Closed`
- Only allow valid transitions
- Frozen/Closed customers cannot open accounts or start new deposits

## Account

### US-B01 Open Account

- Only `Active` customers may open accounts
- Initial balance is zero
- Audit event is created

### US-B02 Get Account Details

- Query by `AccountId` or `AccountNumber`

### US-B03 List Customer Accounts

- Query accounts by `CustomerId`
- Paginated result

### US-B04 Close Account

- Only allowed when balance is zero
- Closed accounts reject new deposits

## Deposit

### US-C01 Create Deposit Request

- Requires customer, account, amount, currency, channel, and `Idempotency-Key`
- Amount must be greater than zero
- Return transaction number and current status

### US-C02 Post Deposit

- Update balances
- Create transaction record
- Publish downstream events

### US-C03 Idempotency Protection

- Same idempotency key must not create multiple posted deposits

### US-C04 Failure Handling

- Capture failure reason and status
- Do not update balance on failed business path

### US-C05 Query Deposit Transaction

- Query by transaction id or number

## Audit

### US-E01 Record Audit Events

- Customer/account/deposit changes produce audit records

### US-E02 Search Audit Logs

- Search by aggregate, actor, action, and time range

### US-E03 Audit Compensation

- Retry and alert when audit write fails

## Platform

### US-F01 Authentication and Authorization

- Roles: `Teller`, `Auditor`, `Admin`

### US-F02 Correlation and Traceability

- Every request carries or generates `CorrelationId`

### US-F03 Health and Monitoring

- Health and readiness endpoints

### US-F04 Concurrency Safety

- Same-account concurrent deposits must preserve correct balance
