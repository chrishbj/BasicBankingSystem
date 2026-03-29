# Final Profile Copy

## Resume Project Entry

### Concise

Built a microservice-based banking platform prototype with `.NET 10`, `ASP.NET Core`, `PostgreSQL`, `RabbitMQ`, and `React`, featuring idempotent deposit APIs, Outbox- and SAGA-style transaction handling, operator tooling, customer self-service flows, and layered automated testing.

### Expanded

Built `BasicBankingSystem`, a full-stack banking platform prototype designed as a technical showcase for distributed systems and production-style engineering patterns. Implemented separate `Customer`, `Account`, `Deposit`, and `Audit` services with `PostgreSQL` persistence and `RabbitMQ`-based asynchronous processing. Designed the deposit workflow around `Idempotency-Key`, `Outbox`, `SAGA-style compensation`, and `PendingReview` recovery, then delivered both an operator-facing console and a customer-facing portal in `React + TypeScript`, backed by unit tests, integration tests, contract tests, Swagger, Docker Compose, and technical documentation.

## LinkedIn Project Description

Built a portfolio banking platform prototype that demonstrates microservice boundaries, idempotent financial APIs, Outbox + RabbitMQ integration, SAGA-style compensation, and dual frontend experiences for bank operators and customers. The project includes `.NET 10` backend services, `PostgreSQL`, `React + TypeScript` frontends, automated tests, OpenAPI/Swagger documentation, Docker Desktop support, and a full set of architectural writeups intended for technical presentation and interview discussion.

## Personal Website / Portfolio Description

`BasicBankingSystem` is a distributed banking platform prototype I built to showcase backend architecture beyond CRUD. The system is split into `Customer`, `Account`, `Deposit`, and `Audit` services, with deposit processing designed around `Idempotency`, `Outbox`, and `SAGA-style compensation`. On top of the backend, I built an operator console for customer/account/review workflows and a separate customer portal for self-service balances, activity, and transaction tracking. The repository also includes automated tests, OpenAPI/Swagger contracts, Docker-based local infrastructure, and detailed technical documentation.

## Suggested “What I Learned” Section

- how to model ownership boundaries in a microservice architecture
- how idempotency and posting references protect financial workflows
- how Outbox and asynchronous messaging reduce integration risk
- how to make distributed transaction failures visible and recoverable
- how to present a project with both code quality and engineering communication in mind
