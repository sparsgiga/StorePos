# AGENTS.md

## Project purpose

StorePos is a Windows desktop POS / sales-journal application for a Georgian construction-materials and hardware store.

The main business goal is to replace handwritten sales notebooks with a fast, reliable digital workflow.

The application must continue working even when the product catalog is incomplete.

## Reference architecture

Use the architecture and coding style of the reference project:

https://github.com/sparsgiga/PersonDirectory

Focus only on architectural conventions, not on its business logic.

Adopt these structural ideas from the reference project:

- Clean Architecture
- Aggregate-oriented Domain layer
- Repository contracts in Domain
- Repository implementations in Persistence
- Separate Persistence and Infrastructure projects
- EF Core configurations via `IEntityTypeConfiguration<T>`
- Unit of Work
- MediatR for application requests
- FluentValidation
- MediatR pipeline behaviors
- Per-layer dependency injection registration
- Thin API endpoints/controllers
- Domain entities with controlled state changes
- Feature-oriented Application folders

Do not copy reference-project business logic.

## Solution structure

Create and maintain this structure:

```text
StorePos.sln

src/
├── StorePos.Domain
├── StorePos.Application
├── StorePos.Persistence
├── StorePos.Infrastructure
├── StorePos.Api
└── StorePos.Desktop

tests/
├── StorePos.Domain.Tests
├── StorePos.Application.Tests
└── StorePos.IntegrationTests
```

## Dependency direction

Expected project references:

```text
StorePos.Domain
    ↑
StorePos.Application
    ↑
StorePos.Infrastructure

StorePos.Persistence -> StorePos.Domain
StorePos.Persistence -> StorePos.Application only if a concrete application abstraction requires it

StorePos.Api -> StorePos.Application
StorePos.Api -> StorePos.Persistence
StorePos.Api -> StorePos.Infrastructure

StorePos.Desktop -> no direct SQL Server or Persistence dependency
```

The Domain project must remain independent of EF Core, WPF, ASP.NET Core, SQL Server, or external systems.

## Domain conventions

Use aggregate-oriented folders.

Suggested structure:

```text
StorePos.Domain/
├── Base/
│   ├── Entity.cs
│   └── IAggregateRoot.cs
│
├── Interfaces/
│   ├── IAudit.cs
│   ├── IRepository.cs
│   ├── IQueryRepository.cs
│   └── IUnitOfWork.cs
│
├── Aggregates/
│   ├── Product/
│   │   ├── Product.cs
│   │   └── IProductRepository.cs
│   │
│   ├── Sale/
│   │   ├── Sale.cs
│   │   ├── SaleItem.cs
│   │   ├── SalePayment.cs
│   │   └── ISaleRepository.cs
│   │
│   ├── Unit/
│   │   ├── Unit.cs
│   │   └── IUnitRepository.cs
│   │
│   └── User/
│       ├── User.cs
│       └── IUserRepository.cs
│
└── Enums/
    ├── SaleStatus.cs
    ├── PaymentType.cs
    └── UserRole.cs
```

`Sale` is an aggregate root.
`SaleItem` and `SalePayment` belong to the `Sale` aggregate.

Do not create separate repositories for `SaleItem` or `SalePayment`.

## Entity base types

Use generic entity identifiers.

Example:

```csharp
public abstract class Entity<TId>
{
    public TId Id { get; protected set; } = default!;
}
```

Repository abstractions must support different ID types.

Prefer:

```csharp
IRepository<TEntity, TId>
IQueryRepository<TEntity, TId>
```

Do not hard-code repository IDs to `int`.

## Audit

Create this Domain interface:

```csharp
public interface IAudit
{
    DateTime DateCreated { get; set; }
    DateTime? DateUpdated { get; set; }
}
```

Entities that require audit timestamps implement `IAudit`.

Audit fields must NOT be assigned manually inside handlers.

`StorePosDbContext.SaveChanges` and `SaveChangesAsync` must automatically set:

- Added entity -> `DateCreated`
- Modified entity -> `DateUpdated`

Use one consistent naming convention everywhere:

```text
DateCreated
DateUpdated
DateCompleted
DateCancelled
```

Do not mix `CreatedAt`, `UpdatedAt`, and `DateCreated`.

## Database conventions

Use SQL Server and EF Core.

General database rules:

- Main entity IDs use `bigint` / `long`
- `Unit.Id` uses `int`
- Enums are persisted as `int`
- Never use `tinyint`
- Every decimal field uses `decimal(18,5)`
- Use `datetime2` for dates
- Product currently has one selling price only
- No product-price-history table in MVP
- Do not create soft-delete infrastructure unless explicitly requested
- `IsActive` is sufficient for Product, Unit, and User
- Avoid database logic in Domain entities

Use migrations for schema changes.

## Initial tables

Create these tables through EF Core migrations:

1. `dbo.Products`
2. `dbo.Units`
3. `dbo.Sales`
4. `dbo.SaleItems`
5. `dbo.SalePayments`
6. `dbo.Users`

Do not add additional business tables unless explicitly requested.

### dbo.Products

```text
Id              bigint          NOT NULL
Code            nvarchar(50)    NOT NULL
Barcode         nvarchar(100)   NULL
Name            nvarchar(300)   NOT NULL
UnitId          int             NOT NULL
Price           decimal(18,5)   NOT NULL
IsActive        bit             NOT NULL
DateCreated     datetime2       NOT NULL
DateUpdated     datetime2       NULL
```

Expected:
- PK on `Id`
- Unique index on `Code`
- Index on `Barcode`
- Search-friendly index strategy for `Name` where appropriate
- FK `UnitId -> Units.Id`

### dbo.Units

```text
Id              int             NOT NULL
Name            nvarchar(100)   NOT NULL
ShortName       nvarchar(20)    NULL
Code            nvarchar(20)    NULL
IsActive        bit             NOT NULL
DateCreated     datetime2       NOT NULL
DateUpdated     datetime2       NULL
```

### dbo.Sales

```text
Id                              bigint          NOT NULL
SaleNumber                      nvarchar(50)    NOT NULL
Status                          int             NOT NULL
CashierId                       bigint          NULL
CustomerName                    nvarchar(300)   NULL
CustomerIdentificationNumber    nvarchar(50)    NULL
TotalAmount                     decimal(18,5)   NOT NULL
Note                            nvarchar(1000)  NULL
DateCreated                     datetime2       NOT NULL
DateUpdated                     datetime2       NULL
DateCompleted                   datetime2       NULL
DateCancelled                   datetime2       NULL
RowVersion                      rowversion      NOT NULL
```

Expected:
- PK on `Id`
- Unique index on `SaleNumber`
- Index on `Status`
- Index on `DateCreated`
- FK `CashierId -> Users.Id`
- Configure `RowVersion` as concurrency token

### dbo.SaleItems

```text
Id              bigint          NOT NULL
SaleId          bigint          NOT NULL
ProductId       bigint          NULL
ProductCode     nvarchar(50)    NULL
Barcode         nvarchar(100)   NULL
ProductName     nvarchar(300)   NOT NULL
UnitId          int             NULL
UnitName        nvarchar(100)   NULL
Quantity        decimal(18,5)   NOT NULL
UnitPrice       decimal(18,5)   NOT NULL
LineTotal       decimal(18,5)   NOT NULL
IsManual        bit             NOT NULL
Note            nvarchar(500)   NULL
DateCreated     datetime2       NOT NULL
DateUpdated     datetime2       NULL
```

Expected:
- FK `SaleId -> Sales.Id`
- FK `ProductId -> Products.Id`, nullable
- FK `UnitId -> Units.Id`, nullable
- index on `SaleId`
- index on `ProductId`

Important business rule:
`ProductId` is nullable by design.

A manually entered product is a valid sale item even when no catalog product exists.

Historical sale data must remain stable.
`ProductName`, `ProductCode`, `Barcode`, `UnitName`, and `UnitPrice` are snapshots stored on the sale item.

### dbo.SalePayments

```text
Id              bigint          NOT NULL
SaleId          bigint          NOT NULL
PaymentType     int             NOT NULL
Amount          decimal(18,5)   NOT NULL
DateCreated     datetime2       NOT NULL
DateUpdated     datetime2       NULL
```

Expected:
- FK `SaleId -> Sales.Id`
- index on `SaleId`

### dbo.Users

```text
Id              bigint          NOT NULL
Username        nvarchar(100)   NOT NULL
DisplayName     nvarchar(200)   NOT NULL
PasswordHash    nvarchar(500)   NULL
Role            int             NOT NULL
IsActive        bit             NOT NULL
DateCreated     datetime2       NOT NULL
DateUpdated     datetime2       NULL
```

Expected:
- PK on `Id`
- Unique index on `Username`

## Enums

Persist all enums as `int`.

Initial enums:

```csharp
public enum SaleStatus
{
    Draft = 1,
    Completed = 2,
    Cancelled = 3
}
```

```csharp
public enum PaymentType
{
    Cash = 1,
    Card = 2,
    BankTransfer = 3,
    Other = 4
}
```

```csharp
public enum UserRole
{
    Cashier = 1,
    Administrator = 2
}
```

## Repository pattern

Follow the repository approach used by the reference architecture, but improve it for generic key types.

Create generic abstractions in Domain.

Example direction:

```csharp
public interface IRepository<TEntity, TId>
    where TEntity : Entity<TId>
{
    Task<TEntity?> GetByIdAsync(
        TId id,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);

    void Update(TEntity entity);

    void Remove(TEntity entity);
}
```

Create a query abstraction separately:

```csharp
public interface IQueryRepository<TEntity, TId>
    where TEntity : Entity<TId>
{
    IQueryable<TEntity> Query();
}
```

If exposing `IQueryable` causes architectural leakage in a specific use case, prefer aggregate-specific query methods instead.

Do not create repository abstractions just to duplicate every EF Core method.

Create aggregate-specific contracts only where useful:

```text
IProductRepository
ISaleRepository
IUnitRepository
IUserRepository
```

Repository implementations belong in `StorePos.Persistence`.

## Unit of Work

Create `IUnitOfWork` in Domain or Application abstraction area consistent with the reference-project style.

It should coordinate:

- `SaveChangesAsync`
- transaction begin
- commit
- rollback

Do not create unnecessary transaction complexity in simple read-only operations.

## Persistence layer

`StorePos.Persistence` owns:

```text
Persistence/
├── Configurations/
├── Context/
├── Migrations/
├── Repositories/
└── DependencyInjection.cs
```

Create:

```text
StorePosDbContext
Repository<TEntity,TId>
QueryRepository<TEntity,TId>
SaleRepository
ProductRepository
UnitRepository
UserRepository
UnitOfWork
```

Entity mapping must use `IEntityTypeConfiguration<T>` classes.

Do not place EF Core attributes on Domain entities unless there is a compelling reason.

Apply configurations through assembly scanning:

```csharp
modelBuilder.ApplyConfigurationsFromAssembly(...)
```

## Application layer

Use MediatR.

Use feature-oriented folders.

Example:

```text
Application/
├── Sales/
│   ├── Commands/
│   │   ├── CreateDraft/
│   │   ├── AddCatalogItem/
│   │   ├── AddManualItem/
│   │   ├── Complete/
│   │   └── Cancel/
│   │
│   └── Queries/
│       ├── GetDrafts/
│       └── GetById/
│
├── Products/
│   └── Queries/
│       ├── Search/
│       └── GetByBarcode/
│
└── Common/
    └── Behaviours/
```

Use:
- MediatR
- FluentValidation
- validation pipeline behavior
- transaction pipeline behavior for requests explicitly marked as transactional

Do not put transaction boilerplate in every handler.

Do not implement business features until the current task explicitly asks for them.

## API layer

API is a thin composition/transport layer.

Responsibilities:
- register layers
- expose application use cases
- map requests/responses
- middleware
- error handling

Controllers/endpoints must not contain business logic.

Preferred flow:

```text
HTTP
-> Controller/Endpoint
-> MediatR
-> Application Handler
-> Repository / UnitOfWork
-> Persistence
```

## Desktop layer

`StorePos.Desktop` is the WPF cashier client.

It must not:
- connect directly to SQL Server
- reference Persistence
- contain business logic in code-behind

It should communicate through the application API.

Use MVVM pragmatically.

The cashier UI must prioritize:
- keyboard use
- barcode scanner input
- multiple open draft sales
- immediate persistence
- crash/restart recovery
- very simple Georgian UI

## Non-negotiable sales rules

1. A sale must never be blocked only because the product is missing from the product catalog.
2. A manual sale item is valid.
3. Several Draft sales may coexist.
4. Draft sales are persisted records, not UI-only tabs.
5. Every important draft change is saved immediately.
6. App restart must restore open Draft sales.
7. Completed historical prices must never change when product prices later change.

## Coding style

- .NET 8
- C#
- Nullable reference types enabled
- Async I/O
- `CancellationToken` propagated
- `Async` suffix for asynchronous methods
- No business logic in controllers or WPF code-behind
- No EF Core in Domain
- No speculative abstractions
- No unnecessary packages
- No microservices
- No message broker
- No event sourcing
- No cloud complexity
- Prefer explicit code over clever generic frameworks

## Test authoring

- Do not create new tests or modify existing test files unless the user explicitly requests test work.
- Existing test projects may still be built or executed for verification when appropriate.

## Current implementation phase

The current phase is infrastructure setup only.

When explicitly asked to implement the initial persistence phase, do exactly this:

1. Create Domain base types/interfaces
2. Create the six initial Domain entities
3. Create enums
4. Add EF Core to Persistence
5. Create `StorePosDbContext`
6. Create `IEntityTypeConfiguration<T>` mappings
7. Create repository abstractions and implementations
8. Create Unit of Work
9. Add audit timestamp handling in `SaveChanges` / `SaveChangesAsync`
10. Add DI registration for Persistence
11. Create the initial migration
12. Verify the full solution builds

Do NOT add application commands/queries or WPF screens during this phase unless explicitly requested.

After implementation:
- run build
- run tests if present
- report created files
- report project references
- report NuGet packages added
- report migration name
- report build result
