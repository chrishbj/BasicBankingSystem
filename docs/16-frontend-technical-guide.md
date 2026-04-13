# Frontend Technical Guide

## Purpose

This document explains the frontend surfaces in the current repository, how they connect to the backend, and which frontend design decisions matter most.

## Current Frontend Surfaces

The repository now contains three frontend applications:

1. `Banking.Web`
   - operator-facing operations console
2. `Banking.CustomerPortal`
   - customer-facing self-service portal
3. `Banking.PlatformOps`
   - platform operations console for monitoring, diagnostics, and maintenance

## 1. Banking.Web

### Purpose

This frontend supports internal operator workflows such as:

- customer browsing and selection
- customer activation
- account opening and account lookup
- deposit submission
- withdrawal submission
- account activity review
- pending-review handling

### Key Structure

- `src/Banking.Web/src/App.tsx`
- `src/Banking.Web/src/hooks/useOperationsConsole.ts`
- `src/Banking.Web/src/components/*`
- `src/Banking.Web/src/api.ts`

### Important Patterns

#### Container + Presentational Split

`App.tsx` and `useOperationsConsole.ts` coordinate screen-level state and actions, while components stay focused on rendering and event wiring.

#### Hook-Based Screen Orchestration

`useOperationsConsole.ts` acts like a lightweight frontend application service. It owns:

- selected customer and account context
- form state
- loading and mutation actions
- status text and toast messages
- polling for in-flight deposit status

#### Browser-to-Gateway Proxy Path

The browser talks to:

- `/customer-api`
- `/account-api`
- `/deposit-api`
- `/audit-api`

This keeps the frontend simple while still exposing service boundaries clearly.

### Current Tradeoffs

- business rules still live mainly in backend services
- frontend validation is intentionally lightweight
- customer/account context drives the workflow more than page-style CRUD does

## 2. Banking.CustomerPortal

### Purpose

This frontend supports customer self-service workflows:

- sign in
- view dashboard summary
- browse accounts
- review account activity
- submit deposits
- submit withdrawals
- track transaction status

### Key Structure

- `src/Banking.CustomerPortal/src/App.tsx`
- `src/Banking.CustomerPortal/src/api.ts`

### Important Patterns

#### BFF-Oriented Frontend

The customer portal does not call all backend services directly. It talks only to the customer portal BFF through:

- `/customer-portal-api/...`

That keeps browser concerns separate from backend aggregation concerns.

#### Session-Aware Browser Client

The API layer uses:

- `credentials: 'include'`

This allows the browser to send the BFF session cookie automatically.

#### Customer-Safe Data Shape

The portal works with customer-facing identifiers such as:

- `customerNumber`
- `accountNumber`
- `transactionNumber`

instead of exposing internal service ids as the primary user-facing handles.

## 3. Banking.PlatformOps

### Purpose

This frontend is a separate platform-oriented control plane for:

- service overview
- dependency and compatibility signals
- rollout and environment views
- deposit workflow monitoring
- correlation diagnostics
- platform maintenance actions
- platform audit visibility

### Key Structure

- `src/Banking.PlatformOps/src/App.tsx`
- `src/Banking.PlatformOps/src/api.ts`

### Important Patterns

#### Gateway-Control-Plane Frontend

This app does not talk to all services separately. It talks to platform endpoints exposed by `Banking.Gateway`, for example:

- `/gateway-api/api/platform/overview`
- `/gateway-api/api/platform/services`
- `/gateway-api/api/platform/workflows/deposits/runtime`

#### Summary + Drill-Through UX

The platform UI is designed to show high-level health and workflow summaries first, and then allow the operator to drill into:

- specific workflow detail
- correlation traces
- maintenance actions

This keeps the console useful without turning it into a generic observability product.

## Shared Frontend Design Decisions

### Separate Frontends By Trust Boundary

The repository keeps:

- operator console
- customer portal
- platform operations console

as separate apps because they serve different users, capabilities, and security expectations.

### Keep Frontends Thin

Core business rules remain in backend services. Frontends focus on:

- workflow guidance
- human-friendly identifiers
- error presentation
- light client-side validation

### Prefer Human-Oriented Workflow Context

Across the frontends, the UX emphasizes:

- customer number
- account number
- transaction number
- review state

instead of exposing internal ids as the main user navigation model.

## Current Limitations

- no dedicated frontend test suite yet
- no shared design system across all three frontends
- no advanced client-side caching layer such as React Query
- role-aware UI trimming is still limited

## Recommended Next Steps

- add frontend tests for key operator, portal, and platform workflows
- unify more shared UI conventions across the three apps
- introduce richer drill-through links between business and platform consoles
- add stronger role-aware UI behavior once the authorization model evolves
