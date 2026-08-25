# StorePos Architecture

## Architectural reference

The StorePos architecture follows the structural conventions of:

https://github.com/sparsgiga/PersonDirectory

The reference is used only for architecture and engineering conventions.

The StorePos business model is independent.

## Layer model

```text
StorePos.Domain
StorePos.Application
StorePos.Persistence
StorePos.Infrastructure
StorePos.Api
StorePos.Desktop
```

### Domain

Owns:
- entities
- aggregates
- enums
- repository contracts
- audit abstraction
- aggregate-root marker
- unit-of-work abstraction where appropriate

Must have no dependency on EF Core or UI frameworks.

### Application

Owns:
- commands
- queries
- handlers
- validators
- MediatR pipeline behaviors
- use-case orchestration

No direct SQL Server code.

### Persistence

Owns:
- EF Core
- SQL Server
- DbContext
- migrations
- entity configurations
- repository implementations
- UnitOfWork implementation
- automatic audit timestamp persistence

### Infrastructure

Reserved for non-database technical concerns and future integrations.

Do not move EF Core persistence into Infrastructure.

### Api

Owns:
- HTTP transport
- composition root
- dependency registration
- controllers/endpoints
- middleware

No business logic.

### Desktop

WPF cashier client.

Owns:
- UI
- scanner/keyboard interaction
- view models
- navigation
- presentation state

No SQL Server connection.
No Persistence reference.

## Aggregate design

### Product aggregate

```text
Product
```

Catalog master record.

### Sale aggregate

```text
Sale
├── SaleItem
└── SalePayment
```

`Sale` is the aggregate root.

`SaleItem` and `SalePayment` are modified through the `Sale` aggregate where practical.

### Unit aggregate

```text
Unit
```

Lookup/master data.

### User aggregate

```text
User
```

Cashier/admin identity.

## Persistence schema

Initial tables:

```text
dbo.Products
dbo.Units
dbo.Sales
dbo.SaleItems
dbo.SalePayments
dbo.Users
```

Relationships:

```text
Units       1 -> N Products
Products    1 -> N SaleItems
Sales       1 -> N SaleItems
Sales       1 -> N SalePayments
Users       1 -> N Sales
```

`SaleItems.ProductId` is nullable.

This is deliberate.

## Domain audit convention

Use:

```csharp
public interface IAudit
{
    DateTime DateCreated { get; set; }
    DateTime? DateUpdated { get; set; }
}
```

The DbContext sets these values automatically.

Handlers must not manually assign audit timestamps.

## Database type rules

```text
Main IDs       -> bigint / long
Unit.Id        -> int
Enums          -> int
Decimals       -> decimal(18,5)
Dates          -> datetime2
```

Never use `tinyint` for enums.

## Repository model

Use both:

```text
IRepository<TEntity,TId>
IQueryRepository<TEntity,TId>
```

Aggregate-specific repositories extend these only where useful.

Repository contracts live in Domain.
Implementations live in Persistence.

## EF Core configuration

Use one configuration class per mapped entity:

```text
ProductConfiguration
UnitConfiguration
SaleConfiguration
SaleItemConfiguration
SalePaymentConfiguration
UserConfiguration
```

All implement:

```csharp
IEntityTypeConfiguration<T>
```

Apply via assembly scan.

## Runtime flow

Later, cashier actions will follow:

```text
WPF Desktop
   ↓
HTTP API
   ↓
MediatR
   ↓
Application Handler
   ↓
Repository / UnitOfWork
   ↓
EF Core / SQL Server
```

The Desktop application never connects directly to SQL Server.

## Current build order

### Phase 1 — Domain + Persistence foundation

Implement:
- Entity<TId>
- IAggregateRoot
- IAudit
- IRepository
- IQueryRepository
- IUnitOfWork
- Product
- Unit
- Sale
- SaleItem
- SalePayment
- User
- enums
- StorePosDbContext
- EF configurations
- repositories
- UnitOfWork
- audit timestamp persistence
- initial migration
- DI registration

No business features yet.

### Phase 2 — Application foundation

Add:
- MediatR
- FluentValidation
- validation behavior
- transactional-request marker
- transaction behavior
- dependency registration

### Phase 3 — First vertical slice

Implement:

```text
CreateDraftSale
```

End-to-end:

```text
API
-> MediatR
-> Handler
-> ISaleRepository
-> UnitOfWork
-> SQL Server
```

### Phase 4 — Manual sale flow

Implement:

```text
AddManualItem
```

This proves the core product idea:
a sale can continue even when there is no matching product record.

### Phase 5 — Desktop

Create WPF cashier shell and connect it to the API.

## Architecture guardrails

Do not:
- put EF Core in Domain
- let WPF reference Persistence
- create separate repositories for SaleItem or SalePayment
- introduce stock management yet
- introduce unnecessary tables
- introduce external catalog/integration terminology yet
- introduce microservices or messaging
- add application features during Persistence-only tasks

## Deployment database initialization

StorePos requires a compatible SQL Server engine/instance to be installed and
reachable. The configured SQL identity must be allowed to create the StorePos
database on first run and to apply EF Core schema changes.

`StorePos.Api` applies existing EF Core migrations during startup. On first run,
EF Core creates the StorePos database and applies every migration. During an
application upgrade, it preserves the database and applies only migrations that
are not recorded in `__EFMigrationsHistory`.

Developers remain responsible for creating new migration files with
`dotnet ef migrations add <MigrationName>`. Cashiers and installers do not need
to create the StorePos database or run `dotnet ef database update` manually.
The application does not use `EnsureCreated` and never deletes or resets the
database automatically.
