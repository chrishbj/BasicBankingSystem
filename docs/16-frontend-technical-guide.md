# Frontend Technical Guide

## Purpose

This document explains how the frontend is built, which technologies it uses, which patterns matter most, and the main design tradeoffs behind the current implementation.

## Scope

The current frontend is an operator-facing React application for:

- customer browsing and selection
- account opening and account lookup by account number
- deposit and withdrawal submission
- account activity history
- pending-review queue operations

## Technology Stack

- `React 19`
- `TypeScript`
- `Vite`
- `Fetch API`
- `Nginx` for Docker hosting and reverse proxying
- plain CSS with component-oriented structure

## Project Structure

- `src/App.tsx`: top-level workspace composition
- `src/components/*`: UI panels and reusable UI elements
- `src/hooks/useOperationsConsole.ts`: central screen orchestration and action state
- `src/api.ts`: HTTP client layer
- `src/types.ts`: frontend API contracts
- `src/utils/*`: formatting helpers

## Important Design Patterns

### Container-Presentational Split

`App.tsx` and `useOperationsConsole.ts` coordinate state and actions, while panel components stay focused on rendering and input wiring.

Why it matters:

- keeps UI components easier to evolve
- avoids duplicating API orchestration logic
- makes future frontend testing simpler

### Hook-Based Screen Orchestration

`useOperationsConsole.ts` acts as a small application service for the operations workspace.

It owns:

- selected customer/account context
- loading and mutation actions
- form state
- polling state
- operator messages and toast notifications

### API Gateway Pattern at the Browser Edge

The frontend talks to service-specific proxy paths such as:

- `/customer-api`
- `/account-api`
- `/deposit-api`
- `/audit-api`

This keeps browser configuration simple and avoids direct cross-origin complexity during local Docker runs.

### Context-Driven Navigation

The UI is organized around a selected customer and selected account rather than isolated CRUD pages.

Why this was chosen:

- better fits operator workflows
- reduces repeated data entry
- makes it easier to move between account, transaction, and review tasks

## Key Design Decisions

### Use Account Number for Human Lookup

The UI avoids operator-facing lookup by internal `accountId`.

Tradeoff:

- we still keep internal ids in the model for service calls
- but user lookup and visible workflows prefer `accountNumber`

This is more realistic for banking operations and reduces confusion.

### Keep the Frontend Thin

Business rules still live primarily in backend services.

The frontend performs lightweight validation only for:

- required fields
- amount shape
- obvious form errors

Tradeoff:

- backend remains the source of truth
- frontend stays easier to maintain
- some validation is intentionally duplicated for usability

### Separate Deposit and Withdrawal Tabs

Even though both use a similar form shape, they are split into separate workspaces.

Why:

- operators treat them as distinct actions
- language and expectations differ
- it reduces accidental misuse

### Progressive Discoverability

The interface emphasizes:

- customer list first
- account list second
- activity and review operations after context is selected

This is intentionally more guided than a generic admin screen.

## Operational UX Considerations

- health indicators stay compact
- long references wrap safely
- major flows surface customer name, customer number, and account number
- review operations keep the currently selected account visible

## Current Limitations

- no dedicated frontend test suite yet
- no client-side caching layer such as React Query
- no role-based UI trimming yet
- no advanced data grid features such as export, sticky filters, or saved views

## Recommended Next Steps

- add a frontend test layer for critical operator flows
- introduce account-number-first search in more places
- add richer review-to-account and deposit-to-account drill-through
- add role-aware UI behavior once authorization requirements are finalized
