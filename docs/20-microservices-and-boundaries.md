# Microservices And Boundaries

## Why Microservices Here

This project splits the backend by business responsibility instead of by technical layer.

The core boundaries are:

- `Customer Service`
- `Account Service`
- `Deposit Service`
- `Audit Service`

This keeps ownership clearer than a single “banking API” would.

## Boundary Design

### Customer Service

Owns:

- customer profile
- lifecycle status
- demo portal sign-in validation

Source:

- `src/Banking.Services.Customer/Controllers/CustomersController.cs`
- `src/Banking.Services.Customer/Services/CustomerService.cs`

### Account Service

Owns:

- account creation
- account number
- balance mutation
- account activity history

This is important because balance ownership should live in exactly one service.

Source:

- `src/Banking.Services.Account/Services/AccountService.cs`
- `src/Banking.Services.Account/Controllers/AccountsController.cs`

### Deposit Service

Owns:

- deposit transaction state
- integration workflow
- pending review and compensation handling

It does not own balances directly. It coordinates with `Account Service`.

Source:

- `src/Banking.Services.Deposit/Services/DepositService.cs`
- `src/Banking.Services.Deposit/Services/DepositTransactionProcessor.cs`

### Audit Service

Owns:

- audit records
- audit query access

Keeping audit separate prevents the main transaction services from becoming responsible for compliance storage.

Source:

- `src/Banking.Services.Audit/Controllers/AuditsController.cs`

## Why Not A Single Database-Centric API

A monolith would be simpler at the start, but weaker for this showcase because it would hide several important distributed-system concerns:

- service boundaries
- eventual consistency
- asynchronous processing
- failure recovery
- internal service authentication

This project intentionally demonstrates those concerns.

## Communication Between Services

HTTP is used for synchronous calls such as:

- `Deposit -> Account`
- `Deposit -> Audit`
- `Account -> Customer`

Relevant source:

- `src/Banking.Services.Deposit/Program.cs`
- `src/Banking.Services.Account/Program.cs`
- `src/Banking.BuildingBlocks/Security/InternalServiceAuthenticationDelegatingHandler.cs`

RabbitMQ is used for asynchronous deposit processing.

Relevant source:

- `src/Banking.Services.Deposit/Messaging/RabbitMqDepositEventPublisher.cs`
- `src/Banking.Services.Deposit/Messaging/RabbitMqDepositMessageConsumer.cs`

## Tradeoffs

Benefits:

- clearer domain ownership
- better long-term scalability
- easier to evolve workflows independently

Costs:

- more moving parts
- cross-service failure handling becomes necessary
- more infrastructure and testing complexity

Those tradeoffs are exactly why this project is useful as a technical portfolio example.
