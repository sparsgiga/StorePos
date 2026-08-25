using Microsoft.EntityFrameworkCore;
using StorePos.Application.Common.Exceptions;
using StorePos.Application.Sales.Commands.AddDebtPayment;
using StorePos.Application.Sales.Commands.Reopen;
using StorePos.Application.Sales.Queries.GetDetails;
using StorePos.Application.Sales.Queries.GetHistory;
using StorePos.Domain.Aggregates.Sale;
using StorePos.Domain.Enums;
using StorePos.Persistence;
using StorePos.Persistence.Context;
using StorePos.Persistence.Queries;
using StorePos.Persistence.Repositories;

namespace StorePos.IntegrationTests.Sales;

public sealed class DebtSaleWorkflowTests
{
    [Fact]
    public async Task LaterDebtPayment_PersistsLocalAuditAndReopenIsRejected()
    {
        await using var context = CreateContext();
        var sale = CreateSale("20260825-0101", 200m, withCustomer: true);
        await context.Sales.AddAsync(sale);
        await context.SaveChangesAsync();
        sale.Complete(
            [new SalePaymentAllocation(PaymentType.Cash, 100m)],
            DateTime.Now,
            allowDebt: true);
        await context.SaveChangesAsync();

        var beforePayment = DateTime.Now;
        var result = await new AddDebtPaymentCommandHandler(
                new SaleRepository(context),
                new UnitOfWork(context))
            .Handle(
                new AddDebtPaymentCommand(sale.Id, PaymentType.Cash, 60m),
                CancellationToken.None);
        var afterPayment = DateTime.Now;

        Assert.NotNull(result);
        Assert.Equal(160m, result.PaidAmount);
        Assert.Equal(40m, result.OutstandingAmount);
        Assert.True(result.HasDebt);
        Assert.Equal(SalePaymentKind.DebtRepayment, result.Payment.PaymentKind);
        Assert.InRange(result.Payment.DateCreated, beforePayment, afterPayment);

        await Assert.ThrowsAsync<SaleOperationConflictException>(() =>
            new ReopenSaleCommandHandler(
                    new SaleRepository(context),
                    new UnitOfWork(context))
                .Handle(new ReopenSaleCommand(sale.Id), CancellationToken.None));

        Assert.Equal(SaleStatus.Completed, sale.Status);
        Assert.Equal(2, sale.Payments.Count);
    }

    [Fact]
    public async Task HistoryProjectsFullPartialAndRepaidDebtStatesSqlSide()
    {
        await using var context = CreateContext();
        var fullyPaid = CreateSale("20260825-0201", 200m, withCustomer: false);
        var partialDebt = CreateSale("20260825-0202", 200m, withCustomer: true);
        var fullDebt = CreateSale("20260825-0203", 200m, withCustomer: true);
        var laterPartial = CreateSale("20260825-0204", 200m, withCustomer: true);
        var laterFullyPaid = CreateSale("20260825-0205", 200m, withCustomer: true);
        await context.Sales.AddRangeAsync(
            fullyPaid, partialDebt, fullDebt, laterPartial, laterFullyPaid);
        await context.SaveChangesAsync();

        var completed = new DateTime(2026, 8, 25, 12, 0, 0);
        fullyPaid.Complete(
            [new SalePaymentAllocation(PaymentType.Cash, 200m)], completed);
        partialDebt.Complete(
            [new SalePaymentAllocation(PaymentType.Cash, 100m)],
            completed,
            allowDebt: true);
        fullDebt.Complete([], completed, allowDebt: true);
        laterPartial.Complete(
            [new SalePaymentAllocation(PaymentType.Cash, 100m)],
            completed,
            allowDebt: true);
        laterPartial.AddDebtPayment(PaymentType.Cash, 60m);
        laterFullyPaid.Complete(
            [new SalePaymentAllocation(PaymentType.Cash, 100m)],
            completed,
            allowDebt: true);
        laterFullyPaid.AddDebtPayment(PaymentType.Card, 100m);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        var history = await new GetSalesHistoryQueryHandler(new SalesReadService(context))
            .Handle(
                new GetSalesHistoryQuery(
                    Status: SaleStatus.Completed,
                    PageNumber: 1,
                    PageSize: 20),
                CancellationToken.None);
        var byNumber = history.Items.ToDictionary(item => item.SaleNumber);

        AssertFinancialState(byNumber["20260825-0201"], 200m, 0m, false);
        AssertFinancialState(byNumber["20260825-0202"], 100m, 100m, true);
        AssertFinancialState(byNumber["20260825-0203"], 0m, 200m, true);
        AssertFinancialState(byNumber["20260825-0204"], 160m, 40m, true);
        AssertFinancialState(byNumber["20260825-0205"], 200m, 0m, false);

        var details = await new GetSaleDetailsQueryHandler(new SalesReadService(context))
            .Handle(new GetSaleDetailsQuery(laterPartial.Id), CancellationToken.None);
        Assert.NotNull(details);
        Assert.Equal(laterPartial.CustomerId, details.CustomerId);
        Assert.Equal(160m, details.PaidAmount);
        Assert.Equal(40m, details.OutstandingAmount);
        Assert.True(details.HasDebt);
        Assert.Collection(
            details.Payments,
            payment => Assert.Equal(SalePaymentKind.Completion, payment.PaymentKind),
            payment => Assert.Equal(SalePaymentKind.DebtRepayment, payment.PaymentKind));
        Assert.All(details.Payments, payment => Assert.NotEqual(default, payment.DateCreated));
    }

    [Fact]
    public async Task AuditCreatedAndUpdatedValuesUseCurrentLocalTime()
    {
        await using var context = CreateContext();
        var sale = Sale.Create("20260825-0301");
        var beforeCreate = DateTime.Now;
        await context.Sales.AddAsync(sale);
        await context.SaveChangesAsync();
        var afterCreate = DateTime.Now;

        Assert.InRange(sale.DateCreated, beforeCreate, afterCreate);
        Assert.Null(sale.DateUpdated);

        var beforeUpdate = DateTime.Now;
        sale.UpdateComment("Updated");
        await context.SaveChangesAsync();
        var afterUpdate = DateTime.Now;

        Assert.NotNull(sale.DateUpdated);
        Assert.InRange(sale.DateUpdated.Value, beforeUpdate, afterUpdate);
    }

    private static Sale CreateSale(string saleNumber, decimal total, bool withCustomer)
    {
        var sale = Sale.Create(saleNumber);
        if (withCustomer)
        {
            sale.AssignCustomer(10, "Customer", null);
        }

        sale.AddManualItem("Item", 1m, total);
        return sale;
    }

    private static void AssertFinancialState(
        SalesHistoryItemModel sale,
        decimal paid,
        decimal outstanding,
        bool hasDebt)
    {
        Assert.Equal(200m, sale.TotalAmount);
        Assert.Equal(paid, sale.PaidAmount);
        Assert.Equal(outstanding, sale.OutstandingAmount);
        Assert.Equal(hasDebt, sale.HasDebt);
    }

    private static StorePosDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<StorePosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new StorePosDbContext(options);
    }
}
