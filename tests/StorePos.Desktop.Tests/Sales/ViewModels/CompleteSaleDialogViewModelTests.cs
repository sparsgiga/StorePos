using StorePos.Desktop.Sales.Models;
using StorePos.Desktop.Sales.ViewModels;

namespace StorePos.Desktop.Tests.Sales.ViewModels;

public sealed class CompleteSaleDialogViewModelTests
{
    [Fact]
    public void FirstTimeDraft_OpensWithEmptyPaymentInputs()
    {
        var viewModel = Create(totalAmount: 100m);

        Assert.Null(viewModel.CashAmount);
        Assert.Null(viewModel.CardAmount);
        Assert.Null(viewModel.BankTransferAmount);
        Assert.Null(viewModel.OtherAmount);
        Assert.Equal(0m, viewModel.PaidAmount);
        Assert.Equal(100m, viewModel.RemainingAmount);
        Assert.False(viewModel.AllowDebt);
        Assert.False(viewModel.CanComplete);
    }

    [Fact]
    public void ReopenedFullyPaidSale_RestoresCashAndCanCompleteWithoutDebt()
    {
        var viewModel = Create(
            100m,
            new PreviousCompletionPaymentStateDto(1, 100m, 0m, 0m, 0m));

        Assert.Equal("100", viewModel.CashAmount);
        Assert.Equal(100m, viewModel.PaidAmount);
        Assert.Equal(0m, viewModel.RemainingAmount);
        Assert.False(viewModel.AllowDebt);
        Assert.True(viewModel.CanComplete);
    }

    [Fact]
    public void ReopenedDebtSale_RestoresNinetyPaidAndTenDebt()
    {
        var viewModel = Create(
            100m,
            new PreviousCompletionPaymentStateDto(1, 90m, 0m, 0m, 0m));

        Assert.Equal("90", viewModel.CashAmount);
        Assert.Equal(90m, viewModel.PaidAmount);
        Assert.Equal(10m, viewModel.RemainingAmount);
        Assert.True(viewModel.AllowDebt);
        Assert.True(viewModel.CanComplete);
    }

    [Fact]
    public void ReopenedMixedPaymentSale_RestoresExactAllocation()
    {
        var viewModel = Create(
            100m,
            new PreviousCompletionPaymentStateDto(1, 40m, 30m, 20m, 0m));

        Assert.Equal("40", viewModel.CashAmount);
        Assert.Equal("30", viewModel.CardAmount);
        Assert.Equal("20", viewModel.BankTransferAmount);
        Assert.Null(viewModel.OtherAmount);
        Assert.Equal(90m, viewModel.PaidAmount);
        Assert.Equal(10m, viewModel.RemainingAmount);
        Assert.True(viewModel.AllowDebt);
    }

    [Theory]
    [InlineData(120, 30)]
    [InlineData(95, 5)]
    public void ReopenedSale_RecalculatesRestoredAllocationAgainstCurrentTotal(
        decimal currentTotal,
        decimal expectedRemaining)
    {
        var viewModel = Create(
            currentTotal,
            new PreviousCompletionPaymentStateDto(1, 90m, 0m, 0m, 0m));

        Assert.Equal(90m, viewModel.PaidAmount);
        Assert.Equal(expectedRemaining, viewModel.RemainingAmount);
        Assert.True(viewModel.AllowDebt);
        Assert.True(viewModel.CanComplete);
    }

    [Fact]
    public void ReopenedSale_CurrentTotalBelowRestoredPayment_ShowsInvalidOverpayment()
    {
        var viewModel = Create(
            80m,
            new PreviousCompletionPaymentStateDto(1, 90m, 0m, 0m, 0m));

        Assert.Equal(90m, viewModel.PaidAmount);
        Assert.Equal(-10m, viewModel.RemainingAmount);
        Assert.False(viewModel.AllowDebt);
        Assert.False(viewModel.CanComplete);
        Assert.NotNull(viewModel.ErrorMessage);
    }

    [Fact]
    public void ReopenedFullDebtSale_UsesVersionSignalDespiteHavingNoPayments()
    {
        var viewModel = Create(
            100m,
            new PreviousCompletionPaymentStateDto(1, 0m, 0m, 0m, 0m));

        Assert.Null(viewModel.CashAmount);
        Assert.Null(viewModel.CardAmount);
        Assert.Equal(0m, viewModel.PaidAmount);
        Assert.Equal(100m, viewModel.RemainingAmount);
        Assert.True(viewModel.AllowDebt);
        Assert.True(viewModel.CanComplete);
    }

    private static CompleteSaleDialogViewModel Create(
        decimal totalAmount,
        PreviousCompletionPaymentStateDto? previousPaymentState = null)
        => new(
            null!,
            saleId: 1,
            totalAmount,
            customerId: 10,
            CancellationToken.None,
            previousPaymentState);
}
