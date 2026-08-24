# StorePos MVP

## Goal

Replace the handwritten sales notebook with a digital cashier workflow.

The MVP succeeds when a cashier can complete every normal store sale digitally even if some items are not yet present in the structured product catalog.

## Core capabilities

1. Scan known product by barcode.
2. Search known product by name or code.
3. Add an unknown item manually using:
   - description
   - quantity
   - unit price
4. Keep several Draft sales open at the same time.
5. Persist every Draft sale.
6. Restore Draft sales after application restart.
7. Complete or cancel a sale.
8. Show daily sales totals.

## Important rule

A missing product record must not block the sale.

`SaleItem.ProductId` is nullable for this reason.

## Initial data model

Tables:

```text
Products
Units
Sales
SaleItems
SalePayments
Users
```

All decimal values use:

```text
decimal(18,5)
```

All persisted enums use:

```text
int
```

## MVP exclusions

Not part of the first version:

- stock enforcement
- opening balances
- purchasing
- suppliers
- product price history
- accounting
- fiscal-device integration
- loyalty
- e-commerce
- AI
- advanced analytics
- external-system write-back
- external-system naming inside the core architecture
