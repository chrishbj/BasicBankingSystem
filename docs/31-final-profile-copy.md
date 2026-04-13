# Final Profile Copy

## Resume Project Entry

### Concise

Built a banking platform prototype with `.NET 10`, `ASP.NET Core`, `PostgreSQL`, `RabbitMQ`, and `React`, featuring gateway/BFF entry architecture, idempotent deposit APIs, outbox- and saga-style transaction handling, platform operations tooling, customer self-service flows, and layered automated testing.

### Expanded

Built `BasicBankingSystem`, a full-stack banking platform prototype designed as a technical showcase for distributed systems and production-style engineering patterns. Implemented separate `Customer`, `Account`, `Deposit`, and `Audit` services with `PostgreSQL` persistence and asynchronous message processing. Designed the deposit workflow around `Idempotency-Key`, `Outbox`, saga-style compensation, and `PendingReview` recovery, then added a dedicated `Gateway`, a customer-facing `BFF`, an operator console, a customer portal, and a separate platform operations console. The repository also includes unit tests, integration tests, contract tests, Swagger, Docker Compose, Newman regression assets, and detailed technical documentation.

## LinkedIn Project Description

Built a portfolio banking platform prototype that demonstrates domain-oriented microservices, gateway and BFF entry patterns, idempotent financial APIs, outbox-based asynchronous processing, saga-style compensation, and separate frontend experiences for operators, customers, and platform operations. The project includes `.NET 10` backend services, `PostgreSQL`, `React + TypeScript` frontends, automated tests, OpenAPI/Swagger documentation, Docker Desktop support, and a full set of architecture and implementation writeups.

## Personal Website / Portfolio Description

`BasicBankingSystem` is a distributed banking platform prototype I built to showcase backend architecture beyond CRUD. The system is split into `Customer`, `Account`, `Deposit`, and `Audit` services, with a `Gateway` for operator and platform-facing entry and a dedicated customer `BFF` for session-based self-service flows. The deposit workflow is designed around `Idempotency`, `Outbox`, and saga-style compensation. On top of the backend, I built an operations console, a customer portal, and a platform operations console. The repository also includes automated tests, OpenAPI/Swagger contracts, Docker-based local infrastructure, and technical documentation designed for technical presentation and interview discussion.

## Suggested “What I Learned” Section

- how to model ownership boundaries in a microservice architecture
- how gateway and BFF patterns support different trust boundaries
- how idempotency, outbox, and saga-style recovery protect financial workflows
- how to make distributed transaction failures visible and recoverable
- how to present a project with both code quality and engineering communication in mind
