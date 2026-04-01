# Customer Portal Overview

## Purpose

The customer portal is a separate frontend from the operations console. It is intended for end customers rather than bank staff.

The current version proves the customer-facing boundary with a dedicated portal frontend and a customer-specific BFF.

## Phase 1 Scope

- demo customer sign-in with `customer number + identity last 4 digits`
- dashboard with portfolio totals and recent activity
- account list and account selection
- grouped activity history
- customer profile display
- self-service deposit and withdrawal
- deposit transaction status tracking

## Why A Separate Frontend

The operations console and customer portal have different goals:

- `Banking.Web` is operator-focused and includes review / recovery tasks
- `Banking.CustomerPortal` is customer-focused and avoids operational controls

Separating them now keeps future authentication, authorization, routing, and deployment cleaner.

## Customer BFF Boundary

The portal now calls `Banking.Bff.CustomerPortal`, which then calls downstream services on behalf of the signed-in customer.

Portal entry APIs now include:

- `POST /api/customer-portal/auth/sign-in`
- `POST /api/customer-portal/auth/sign-out`
- `GET /api/customer-portal/auth/me`
- `GET /api/customer-portal/dashboard`
- `GET /api/customer-portal/accounts`
- `GET /api/customer-portal/accounts/{accountNumber}`
- `GET /api/customer-portal/accounts/{accountNumber}/activities`
- `POST /api/customer-portal/deposits`
- `POST /api/customer-portal/accounts/{accountNumber}/withdrawals`
- `GET /api/customer-portal/transactions`
- `GET /api/customer-portal/transactions/{transactionNumber}`

The BFF then uses existing backend services such as:

- `POST /api/v1/customers/portal-sign-in`
- `GET /api/v1/accounts`
- `GET /api/v1/accounts/{accountId}`
- `GET /api/v1/accounts/{accountId}/activities`
- `POST /api/v1/deposits`
- `GET /api/v1/deposits`
- `GET /api/v1/deposits/{transactionId}`
- `POST /api/v1/accounts/{accountId}/withdrawals`

This keeps the first version thin while a more customer-specific API surface is still being defined.

## Technical Stack

- `React 19`
- `TypeScript`
- `Vite`
- `Fetch API`
- `Nginx` for Docker hosting

## Design Direction

The portal uses customer-facing concepts first:

- `customer name`
- `customer number`
- `account number`
- balances and activity history

It intentionally hides review, compensation, and other back-office operations.

## Key Tradeoffs

### Demo Sign-In Before Real IAM

The portal now uses a dedicated demo sign-in endpoint instead of a customer selector.

Why:

- keeps raw identity numbers out of the customer directory response
- helps validate navigation and customer-facing UX now
- avoids blocking frontend progress on IAM design
- keeps the first delivery lightweight

Current behavior:

- input: `customer number + identity last 4 digits`
- backend validation: `POST /api/v1/customers/portal-sign-in`
- non-standard demo identities are normalized by extracting digits and left-padding to four digits
  - example: `WITHDRAW-DEMO-001` becomes `0001`

### Customer Portal Uses A Dedicated BFF

The browser no longer calls the domain services directly.

Why:

- keeps authentication and session handling server-side
- lets the portal work only with `customerNumber`, `accountNumber`, and `transactionNumber`
- prevents internal IDs from becoming part of the public UI contract
- creates a cleaner path toward future IAM or OIDC

Tradeoff:

- adds one more deployable service
- requires a small aggregation layer to stay in sync with downstream APIs

## Recommended Next Steps

- evolve demo sign-in toward real customer authentication
- keep expanding customer-scoped BFF APIs
- add statement download and notification settings
- add self-service deposit and withdrawal journeys with customer-safe validation
