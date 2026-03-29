# Resume Project Bullets

## Short Version

- Built a microservice-based banking platform prototype with `.NET 10`, `ASP.NET Core`, `PostgreSQL`, `RabbitMQ`, and `React`, covering customer onboarding, account management, deposit processing, audit logging, and customer self-service flows.
- Designed a deposit workflow using `Idempotency-Key`, `Outbox`, and `SAGA-style compensation`, including `PendingReview` recovery paths for partial-failure scenarios.
- Delivered both an operator-facing console and a customer-facing portal, with OpenAPI/Swagger integration, Docker-based local environments, and layered automated tests using `xUnit`, `FluentAssertions`, and `WebApplicationFactory`.

## Medium Version

- Built `BasicBankingSystem`, a distributed banking prototype with separate `Customer`, `Account`, `Deposit`, and `Audit` services, demonstrating bounded-context-oriented microservice design.
- Implemented resilient financial transaction handling with idempotent deposit APIs, persisted outbox messages, RabbitMQ-based async processing, and SAGA-style compensation plus manual review workflows.
- Developed two React frontends: an operations console for staff workflows and a customer portal for self-service account, activity, deposit, and withdrawal scenarios.
- Added automated quality gates through unit tests, integration tests, OpenAPI contract tests, Postman/Newman regression assets, and Docker Desktop end-to-end validation.

## Interview-Focused Version

- I built a portfolio banking system to demonstrate distributed backend architecture beyond CRUD, including microservices, eventual consistency, compensation handling, and API contract discipline.
- The most important technical decision was keeping balance ownership in `Account Service` while letting `Deposit Service` coordinate the transaction workflow through idempotency, outbox dispatch, and SAGA-style processing.
- I also treated documentation and testing as part of the project itself, with OpenAPI specs, Swagger support, integration tests, Newman flows, and clear technical writeups tied to the source code.
