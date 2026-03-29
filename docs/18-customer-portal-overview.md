# Customer Portal Overview

## Purpose

The customer portal is a separate frontend from the operations console. It is intended for end customers rather than bank staff.

The current first version is a demo portal that proves the UI boundary, customer-facing language, and reuse of existing backend services.

## Phase 1 Scope

- demo customer sign-in by choosing an existing customer
- dashboard with portfolio totals and recent activity
- account list and account selection
- grouped activity history
- customer profile display

## Why A Separate Frontend

The operations console and customer portal have different goals:

- `Banking.Web` is operator-focused and includes review / recovery tasks
- `Banking.CustomerPortal` is customer-focused and avoids operational controls

Separating them now keeps future authentication, authorization, routing, and deployment cleaner.

## Reused Backend APIs

The current portal reuses:

- `GET /api/v1/customers`
- `GET /api/v1/accounts`
- `GET /api/v1/accounts/{accountId}`
- `GET /api/v1/accounts/{accountId}/activities`

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

### Demo Sign-In First

The portal currently uses a customer selector instead of real authentication.

Why:

- helps validate navigation and customer-facing UX now
- avoids blocking frontend progress on IAM design
- keeps the first delivery lightweight

### Reuse Existing Services Before Adding BFF APIs

The portal currently aggregates data client-side from existing services.

Why:

- faster to validate customer journeys
- avoids premature backend expansion

Tradeoff:

- the browser currently does more orchestration than a future production portal should

## Recommended Next Steps

- add real customer authentication
- introduce customer-scoped APIs or a portal BFF
- add statement download and notification settings
- add self-service deposit and withdrawal journeys with customer-safe validation
