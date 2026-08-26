using Microsoft.EntityFrameworkCore;
using StorePos.Application.Sales.Commands.Complete;
using StorePos.Application.Sales.Queries.GetDraftDetails;
using StorePos.Domain.Aggregates.Sale;
using StorePos.Domain.Enums;
using StorePos.Persistence;
using StorePos.Persistence.Context;
using StorePos.Persistence.Repositories;

namespace StorePos.IntegrationTests.Sales;

public sealed class PreviousCompletionPaymentStateWorkflowTests
{
    [Fact]
    public async Task ReopenedDraft_IncludesCompletionAndDebtRepaymentInEffectiveAllocation()
    {
        await using var context = CreateContext();
        var sale = await CreatePersistedSaleAsync(context, "PREFILL-DEBT", 1000m);
        sale.AssignCustomer(10, "Customer", null);
        sale.Complete(
            [new SalePaymentAllocation(PaymentType.Cash, 500m)],
            DateTime.Now,
            allowDebt: true);
        sale.AddDebtPayment(Guid.NewGuid(), PaymentType.Card, 200m);
        await context.SaveChangesAsync();
        sale.Reopen();
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var details = await GetDetailsAsync(context, sale.Id);
        var state = Assert.IsType<PreviousCompletionPaymentStateModel>(
            details?.PreviousCompletionPaymentState);

        Assert.Equal(500m, state.CashAmount);
        Assert.Equal(200m, state.CardAmount);
        Assert.Equal(0m, state.BankTransferAmount);
        Assert.Equal(0m, state.OtherAmount);
        Assert.Equal(700m, state.CashAmount + state.CardAmount);
        Assert.Equal(700m, details!.PaidAmount);
        Assert.Equal(300m, details.OutstandingAmount);
        Assert.True(details.HasDebt);
    }

    [Fact]
    public async Task ReopenedDraft_AfterTotalEditKeepsEffectiveAllocationAndRefreshesDebt()
    {
        await using var context = CreateContext();
        var sale = await CreatePersistedSaleAsync(context, "PREFILL-DEBT-EDIT", 1000m);
        sale.AssignCustomer(10, "Customer", null);
        sale.Complete(
            [new SalePaymentAllocation(PaymentType.Cash, 500m)],
            DateTime.Now,
            allowDebt: true);
        sale.AddDebtPayment(Guid.NewGuid(), PaymentType.Card, 200m);
        await context.SaveChangesAsync();
        sale.Reopen();
        var item = Assert.Single(sale.Items);
        sale.UpdateItem(item.Id, item.ProductName, 1m, 900m);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var details = await GetDetailsAsync(context, sale.Id);
        var state = Assert.IsType<PreviousCompletionPaymentStateModel>(
            details?.PreviousCompletionPaymentState);

        Assert.Equal(500m, state.CashAmount);
        Assert.Equal(200m, state.CardAmount);
        Assert.Equal(700m, details!.PaidAmount);
        Assert.Equal(200m, details.OutstandingAmount);
    }

    [Fact]
    public async Task FirstTimeDraft_ReturnsNoPreviousPaymentState()
    {
        await using var context = CreateContext();
        var sale = Sale.Create("PREFILL-NEW");
        await context.Sales.AddAsync(sale);
        await context.SaveChangesAsync();
        sale.AddManualItem("Item", 1m, 100m);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var details = await GetDetailsAsync(context, sale.Id);

        Assert.NotNull(details);
        Assert.Equal(0, details.CompletionVersion);
        Assert.Null(details.PreviousCompletionPaymentState);
    }

    [Fact]
    public async Task ReopenedDraft_ReturnsExactLatestMixedCompletionAllocation()
    {
        await using var context = CreateContext();
        var sale = await CreatePersistedSaleAsync(context, "PREFILL-MIXED", 100m);
        sale.AssignCustomer(10, "Customer", null);
        sale.Complete(
            [
                new SalePaymentAllocation(PaymentType.Cash, 40m),
                new SalePaymentAllocation(PaymentType.Card, 30m),
                new SalePaymentAllocation(PaymentType.BankTransfer, 20m)
            ],
            DateTime.Now,
            allowDebt: true);
        await context.SaveChangesAsync();
        sale.Reopen();
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var details = await GetDetailsAsync(context, sale.Id);
        var state = Assert.IsType<PreviousCompletionPaymentStateModel>(
            details?.PreviousCompletionPaymentState);

        Assert.Equal(1, state.CompletionVersion);
        Assert.Equal(40m, state.CashAmount);
        Assert.Equal(30m, state.CardAmount);
        Assert.Equal(20m, state.BankTransferAmount);
        Assert.Equal(0m, state.OtherAmount);
    }

    [Fact]
    public async Task ReopenedFullDebtSale_ReturnsNonNullZeroPaymentState()
    {
        await using var context = CreateContext();
        var sale = await CreatePersistedSaleAsync(context, "PREFILL-ZERO", 100m);
        sale.AssignCustomer(10, "Customer", null);
        sale.Complete([], DateTime.Now, allowDebt: true);
        await context.SaveChangesAsync();
        sale.Reopen();
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var details = await GetDetailsAsync(context, sale.Id);
        var state = Assert.IsType<PreviousCompletionPaymentStateModel>(
            details?.PreviousCompletionPaymentState);

        Assert.Equal(1, state.CompletionVersion);
        Assert.Equal(0m, state.CashAmount);
        Assert.Equal(0m, state.CardAmount);
        Assert.Equal(0m, state.BankTransferAmount);
        Assert.Equal(0m, state.OtherAmount);
    }

    [Fact]
    public async Task MultipleReopens_ReturnOnlyLatestVersionAllocation()
    {
        await using var context = CreateContext();
        var sale = await CreatePersistedSaleAsync(context, "PREFILL-LATEST", 120m);
        sale.Complete(
            [new SalePaymentAllocation(PaymentType.Cash, 120m)],
            DateTime.Now);
        await context.SaveChangesAsync();
        sale.Reopen();
        sale.Complete(
            [
                new SalePaymentAllocation(PaymentType.Cash, 50m),
                new SalePaymentAllocation(PaymentType.Card, 70m)
            ],
            DateTime.Now.AddMinutes(1));
        await context.SaveChangesAsync();
        sale.Reopen();
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var details = await GetDetailsAsync(context, sale.Id);
        var state = Assert.IsType<PreviousCompletionPaymentStateModel>(
            details?.PreviousCompletionPaymentState);

        Assert.Equal(2, state.CompletionVersion);
        Assert.Equal(50m, state.CashAmount);
        Assert.Equal(70m, state.CardAmount);
        Assert.Equal(120m, state.CashAmount + state.CardAmount);
    }

    [Fact]
    public async Task RecompleteWithRestoredAllocation_InsertsNewVersionAndPreservesOldRow()
    {
        await using var context = CreateContext();
        var sale = await CreatePersistedSaleAsync(context, "PREFILL-RECOMPLETE", 100m);
        sale.AssignCustomer(10, "Customer", null);
        sale.Complete(
            [new SalePaymentAllocation(PaymentType.Cash, 90m)],
            DateTime.Now,
            allowDebt: true);
        await context.SaveChangesAsync();
        var oldPayment = Assert.Single(sale.Payments);
        var oldPaymentId = oldPayment.Id;
        var oldAmount = oldPayment.Amount;
        sale.Reopen();
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var details = await GetDetailsAsync(context, sale.Id);
        var state = Assert.IsType<PreviousCompletionPaymentStateModel>(
            details?.PreviousCompletionPaymentState);
        var repository = new SaleRepository(context);
        var result = await new CompleteSaleCommandHandler(
                repository,
                new UnitOfWork(context),
                TimeProvider.System)
            .Handle(
                new CompleteSaleCommand(
                    sale.Id,
                    [new CompleteSalePayment(PaymentType.Cash, state.CashAmount)],
                    AllowDebt: true),
                CancellationToken.None);

        Assert.NotNull(result);
        context.ChangeTracker.Clear();
        var persisted = await context.Sales
            .Include(current => current.Payments)
            .SingleAsync(current => current.Id == sale.Id);
        Assert.Equal(2, persisted.CompletionVersion);
        Assert.Equal(2, persisted.Payments.Count);
        var preserved = persisted.Payments.Single(payment => payment.Id == oldPaymentId);
        Assert.Equal(1, preserved.CompletionVersion);
        Assert.Equal(oldAmount, preserved.Amount);
        Assert.Equal(90m, persisted.Payments.Single(payment =>
            payment.CompletionVersion == 2).Amount);
        Assert.Equal(90m, persisted.PaidAmount);
        Assert.Equal(10m, persisted.OutstandingAmount);
    }

    private static async Task<Sale> CreatePersistedSaleAsync(
        StorePosDbContext context,
        string saleNumber,
        decimal totalAmount)
    {
        var sale = Sale.Create(saleNumber);
        await context.Sales.AddAsync(sale);
        await context.SaveChangesAsync();
        sale.AddManualItem("Item", 1m, totalAmount);
        await context.SaveChangesAsync();
        return sale;
    }

    private static Task<DraftSaleDetailsModel?> GetDetailsAsync(
        StorePosDbContext context,
        long saleId)
        => new GetDraftSaleDetailsQueryHandler(new SaleRepository(context))
            .Handle(new GetDraftSaleDetailsQuery(saleId), CancellationToken.None);

    private static StorePosDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<StorePosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new StorePosDbContext(options);
    }
}
