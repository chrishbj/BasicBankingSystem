# Request Protection and Idempotency Strategy

## Goal

This project already treats financial writes as idempotent where duplicate effects would be unacceptable.
The next step is to formalize a broader request-protection strategy so that:

- legitimate retries remain safe
- repeated duplicate sends do not create extra financial effects
- abusive or buggy request storms are slowed down
- read APIs are not penalized with write-oriented behavior

The key design choice is that **idempotency** and **request throttling** are related but not the same concern.

## Core Decision

We do **not** apply one identical policy to every endpoint.

Instead, we split protection into two layers:

1. A **global request-rate protection** layer for all requests.
2. A **write-specific replay protection** layer for idempotent writes.

This keeps the platform safe without degrading normal read traffic or valid client retry behavior.

## Why Not Treat Every Request Like `deposit`

`deposit` is a financial write. Its main risk is duplicate business effect.

That is different from:

- a `GET` request, where the main risk is traffic volume
- a login endpoint, where the main risk is abuse or credential attacks
- a profile update, where the main risk is accidental duplicate submission but not double-posting money

If we gave every endpoint the exact same duplicate-delay behavior:

- read APIs would become slower for no business reason
- healthy client retries would be punished too aggressively
- the design would mix business safety concerns with generic traffic management

## Recommended Policy by Endpoint Type

### 1. Financial and High-Impact Writes

Examples:

- deposit
- withdrawal
- transfer
- external callback/webhook endpoints that can trigger money movement or workflow transitions

Policy:

- require or strongly prefer `Idempotency-Key`
- store enough state to replay the original result safely
- return the original logical result for repeated sends
- add progressive backoff only when the same idempotent write is sent repeatedly in a short window

Why:

- duplicate effect is the most important failure to prevent
- retries are normal, especially after timeouts
- request storms still need to be softened

### 2. Non-Financial Writes

Examples:

- create/update customer profile
- account metadata changes
- operator workflow commands without direct money movement

Policy:

- use uniqueness constraints and/or idempotency where duplicates are plausible
- apply standard request-rate protection
- do not automatically apply strong replay backoff unless the endpoint shows real duplicate-submit risk

Why:

- the cost of duplicate creation may be operationally annoying, but usually not financially catastrophic

### 3. Read APIs

Examples:

- query deposit status
- list customers
- account balance reads

Policy:

- apply request-rate protection
- consider caching where appropriate
- do not use write-style idempotency replay behavior

Why:

- reads need availability and responsiveness
- duplicate-read delay does not add meaningful safety

### 4. Security-Sensitive Endpoints

Examples:

- login
- password reset
- OTP or verification-code endpoints

Policy:

- stronger rate limiting
- possible progressive delay or lockout behavior
- separate security controls from financial idempotency controls

Why:

- these endpoints face abuse patterns that differ from ordinary API retries

## Specific Trade-Off: Duplicate Requests With the Same `Idempotency-Key`

For idempotent write APIs, there are two competing goals:

1. Make valid retries complete quickly.
2. Reduce waste or abuse from repeated duplicate sends.

The chosen compromise is:

- first replay should still be fast
- additional short-window replays should become progressively slower
- the response should still describe the same logical operation, not create a new one

This means the platform says:

> "Your duplicate request is safe, but if you keep sending it repeatedly in a tight loop, we will slow the loop down."

## Response Behavior

For repeated idempotent writes:

- return the same transaction/resource identity where possible
- include replay metadata headers so callers can see the request was a replay
- include `Retry-After` when backoff is applied

Suggested response semantics:

- original request still processing: return `202 Accepted` with the original transaction identifier
- original request completed: return the original logical result
- repeated replay storm: still return the original logical result, but add progressive delay first

## Selected Implementation for This Repository

The implementation chosen for this project is:

### Global protection for all requests

- add a shared rate-limiter at the HTTP entry layer
- scope it per caller identity when possible, otherwise fall back to remote IP or `"anonymous"`
- use this as the default protection for all endpoints

### Replay protection only for idempotent writes

- inspect write methods: `POST`, `PUT`, `PATCH`, `DELETE`
- only activate replay backoff when an `Idempotency-Key` header is present
- track repeated sends for the same caller + method + path + idempotency key within a short window
- allow the first replay immediately
- apply bounded exponential backoff to later replays

This gives us:

- broad protection for the whole platform
- business-aware replay handling only where it makes sense
- reusable infrastructure instead of deposit-specific special cases

## Why This Choice Fits the Current Codebase

The repository already demonstrates:

- idempotent deposit creation
- outbox-driven async processing
- SAGA-style compensation

Adding a shared request-protection layer is a natural extension because it:

- preserves the existing deposit business contract
- avoids scattering rate-limit logic across controllers
- makes future write APIs like withdrawal or transfer easier to harden consistently

## Future Extensions

Likely next improvements:

- per-endpoint policy overrides
- stronger security policies for authentication endpoints
- distributed replay tracking instead of in-memory tracking
- observability metrics for rate-limit hits and replay backoff
- API documentation that explicitly describes replay headers and retry expectations
