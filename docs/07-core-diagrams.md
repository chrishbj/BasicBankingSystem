# Current Core Diagrams

The Chinese Mermaid diagrams are available in [docs/ch-cn/07-core-diagrams.zh-CN.md](/E:/DemoProjects/BasicBankingSystem/docs/ch-cn/07-core-diagrams.zh-CN.md).

This document reflects the current implementation in the repository, not the earlier Phase 1 proposal.

Included diagrams:

- Runtime architecture
- Operations console request path
- Customer portal request path
- Deposit workflow
- Deposit review and retry flow
- Customer state diagram
- Account state diagram
- Deposit transaction state diagram
- Service boundary diagram
- Domain relationship diagram

## 1. Runtime Architecture

```mermaid
flowchart LR
  subgraph Frontends["Frontend Layer"]
    Web["Banking.Web<br/>Operations Console"]
    Portal["Banking.CustomerPortal<br/>Customer Portal"]
  end

  subgraph Entry["Entry Layer"]
    Gateway["Banking.Gateway"]
    BFF["Banking.Bff.CustomerPortal"]
  end

  subgraph Services["Backend Services"]
    CustomerSvc["Customer Service"]
    AccountSvc["Account Service"]
    DepositSvc["Deposit Service"]
    AuditSvc["Audit Service"]
  end

  subgraph Storage["Service Databases"]
    CustomerDb[("customers")]
    AccountDb[("accounts<br/>account_postings")]
    DepositDb[("deposit_transactions<br/>deposit_outbox_messages")]
    AuditDb[("audit_logs")]
  end

  subgraph Async["Async Runtime"]
    Outbox["DepositOutboxDispatcher"]
    Consumer["RabbitMqDepositMessageConsumer<br/>or InMemoryDepositMessageConsumer"]
    RetryWorker["DepositPendingReviewRetryWorker"]
    MQ["RabbitMQ / InMemory Queue"]
  end

  Web --> Gateway
  Portal --> BFF

  Gateway --> CustomerSvc
  Gateway --> AccountSvc
  Gateway --> DepositSvc
  Gateway --> AuditSvc

  BFF --> CustomerSvc
  BFF --> AccountSvc
  BFF --> DepositSvc

  CustomerSvc --> CustomerDb
  AccountSvc --> AccountDb
  DepositSvc --> DepositDb
  AuditSvc --> AuditDb

  DepositSvc --> Outbox
  Outbox --> MQ
  MQ --> Consumer
  Consumer --> DepositSvc
  DepositSvc --> RetryWorker
```

## 2. Operations Console Request Path

```mermaid
sequenceDiagram
  participant User as Operator
  participant Web as Banking.Web
  participant GW as Banking.Gateway
  participant Svc as Downstream Service

  User->>Web: Click action in operations console
  Web->>GW: Call /customer-api /account-api /deposit-api /audit-api
  GW->>GW: Validate X-Api-Key
  GW->>Svc: Forward request with internal service identity headers
  Svc-->>GW: Return API response
  GW-->>Web: Return proxied response
```

## 3. Customer Portal Request Path

```mermaid
sequenceDiagram
  participant User as Customer
  participant Portal as Banking.CustomerPortal
  participant BFF as Customer Portal BFF
  participant CS as Customer Service
  participant AS as Account Service
  participant DS as Deposit Service

  User->>Portal: Sign in / browse dashboard
  Portal->>BFF: Call /customer-portal-api with session cookie
  BFF->>BFF: Restore customer from session
  BFF->>CS: Read customer profile
  BFF->>AS: Read accounts / activities
  BFF->>DS: Read deposits / create deposit
  BFF-->>Portal: Return frontend-friendly aggregated payload
```

## 4. Deposit Workflow

```mermaid
flowchart TD
  A["Client submits deposit"] --> B["Deposit API validates request"]
  B --> C{"Idempotency-Key exists?"}
  C -- Yes --> D["Return existing transaction"]
  C -- No --> E["Create DepositTransaction<br/>Status=Received"]
  E --> F["Create DepositOutboxMessage"]
  F --> G["Save both in one DB transaction"]
  G --> H["Return 202 Accepted"]
  H --> I["DepositOutboxDispatcher polls outbox"]
  I --> J["Publish DepositRequested message"]
  J --> K["Consumer loads transaction"]
  K --> L["DepositTransactionProcessor starts"]
  L --> M["Call Account Service to post deposit"]
  M --> N{"Posting succeeded?"}
  N -- No --> O["Status=Rejected or Failed"]
  N -- Yes --> P["Write audit log"]
  P --> Q{"Audit succeeded?"}
  Q -- Yes --> R["Status=Succeeded"]
  Q -- No --> S["Enter compensation path"]
```

## 5. Deposit Review And Retry Flow

```mermaid
flowchart TD
  A["Audit/post-processing failure after account posting"] --> B["CompensationStatus=InProgress"]
  B --> C["Call Account Service deposit reversal"]
  C --> D{"Compensation succeeded?"}
  D -- Yes --> E["Status=Reversed"]
  D -- No --> F["Status=PendingReview"]
  F --> G["DepositPendingReviewRetryWorker scans queue"]
  G --> H{"Automatic retry allowed?"}
  H -- Yes --> I["Retry compensation"]
  H -- No --> J["Wait for platform action"]
  J --> K["Gateway platform maintenance API"]
  K --> L["Retry compensation / resolve review / requeue outbox"]
```

## 6. Customer State Diagram

```mermaid
stateDiagram-v2
  [*] --> Pending
  Pending --> Active
  Active --> Frozen
  Frozen --> Active
  Active --> Closed
  Pending --> [*]
  Closed --> [*]
```

## 7. Account State Diagram

```mermaid22 
stateDiagram-v2
  [*] --> Active
  Active --> Frozen
  Frozen --> Active
  Active --> Closed
  Closed --> [*]
```

## 8. Deposit Transaction State Diagram

```mermaid
stateDiagram-v2
  [*] --> Received
  Received --> Processing
  Processing --> Succeeded
  Processing --> Rejected
  Processing --> Failed
  Processing --> PendingReview
  PendingReview --> Reversed
  PendingReview --> Succeeded
  PendingReview --> FailedExternally: review resolution
  Rejected --> [*]
  Failed --> [*]
  Succeeded --> [*]
  Reversed --> [*]
```

## 9. Service Boundary Diagram

```mermaid
flowchart LR
  subgraph Access["Access Layer"]
    Gateway["Gateway"]
    BFF["Customer Portal BFF"]
  end

  subgraph CustomerContext["Customer Context"]
    CustomerSvc["Customer Service"]
  end

  subgraph AccountContext["Account Context"]
    AccountSvc["Account Service"]
  end

  subgraph DepositContext["Deposit Context"]
    DepositSvc["Deposit Service"]
    Outbox["Outbox + Consumer + Retry Worker"]
  end

  subgraph AuditContext["Audit Context"]
    AuditSvc["Audit Service"]
  end

  Gateway --> CustomerSvc
  Gateway --> AccountSvc
  Gateway --> DepositSvc
  Gateway --> AuditSvc

  BFF --> CustomerSvc
  BFF --> AccountSvc
  BFF --> DepositSvc

  DepositSvc --> AccountSvc
  DepositSvc --> AuditSvc
  DepositSvc --> Outbox
```

## 10. Domain Relationship Diagram

```mermaid
erDiagram
    customers ||--o{ accounts : logical_customer
    accounts ||--o{ account_postings : posting_history
    customers ||--o{ deposit_transactions : logical_depositor
    accounts ||--o{ deposit_transactions : target_account
    deposit_transactions ||--o{ deposit_outbox_messages : outbox_events
```

## Recommended Review Set

For the current codebase, present these six first:

1. Runtime architecture
2. Operations console request path
3. Customer portal request path
4. Deposit workflow
5. Deposit review and retry flow
6. Deposit transaction state diagram
