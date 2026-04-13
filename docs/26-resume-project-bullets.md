# Resume Project Bullets

## Short Version

- Built a banking platform prototype with `.NET 10`, `ASP.NET Core`, `PostgreSQL`, `RabbitMQ`, and `React`, covering customer, account, deposit, audit, customer self-service, and platform operations workflows.
- Designed a resilient deposit workflow using `Idempotency-Key`, `Outbox`, and `Saga-style compensation`, with `PendingReview` recovery for partial-failure scenarios.
- Delivered separate gateway, BFF, operator console, customer portal, and platform operations experiences, backed by layered automated testing and OpenAPI/Swagger documentation.

## Medium Version

- Built `BasicBankingSystem`, a distributed banking prototype with separate `Customer`, `Account`, `Deposit`, and `Audit` services, plus a dedicated `Gateway` and customer-facing `BFF`.
- Implemented resilient financial transaction handling with idempotent deposit APIs, persisted outbox messages, asynchronous message processing, saga-style compensation, and retry/review recovery paths.
- Developed three React frontends: an operations console for staff workflows, a customer portal for self-service flows, and a platform operations console for monitoring, diagnostics, and maintenance.
- Added automated quality gates through unit tests, integration tests, OpenAPI contract tests, Postman/Newman regression assets, and Docker Desktop end-to-end validation.

## Interview-Focused Version

- I built a portfolio banking system to demonstrate distributed backend architecture beyond CRUD, including microservices, gateway/BFF entry patterns, eventual consistency, and operational recovery.
- The most important technical decision was keeping balance ownership in `Account Service` while letting `Deposit Service` coordinate the workflow through idempotency, outbox dispatch, and saga-style processing.
- I also treated documentation, testing, and presentation as part of the engineering work, with OpenAPI specs, Swagger support, integration tests, Newman flows, and architecture writeups tied directly to the source code.
