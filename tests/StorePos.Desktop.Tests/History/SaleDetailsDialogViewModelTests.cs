using StorePos.Desktop.Common;
using StorePos.Desktop.History.Dialogs;
using StorePos.Desktop.History.Models;
using StorePos.Desktop.History.ViewModels;

namespace StorePos.Desktop.Tests.History;

public sealed class SaleDetailsDialogViewModelTests
{
    [Fact]
    public void CopySaleNumber_CopiesOnlyExactNumber()
    {
        var clipboard = new RecordingClipboardService();
        var viewModel = new SaleDetailsDialogViewModel(
            CreateDetails("20260825-0017"),
            clipboard,
            new StubHistoryDialogService(),
            CancellationToken.None);

        viewModel.CopySaleNumberCommand.Execute(null);

        Assert.Equal("20260825-0017", clipboard.Text);
    }

    [Fact]
    public void CopySaleNumber_EmptyValueDoesNotExecuteOrCrash()
    {
        var clipboard = new RecordingClipboardService();
        var viewModel = new SaleDetailsDialogViewModel(
            CreateDetails(string.Empty),
            clipboard,
            new StubHistoryDialogService(),
            CancellationToken.None);

        Assert.False(viewModel.CopySaleNumberCommand.CanExecute(null));
        viewModel.CopySaleNumberCommand.Execute(null);
        Assert.Null(clipboard.Text);
    }

    [Fact]
    public void PaymentGroups_SeparateCurrentAndPreviousCompletionPayments()
    {
        var details = CreateDetails("20260825-0018") with
        {
            CompletionVersion = 2,
            Payments =
            [
                new SaleDetailsPaymentDto(1, 1, 1, 100m, new DateTime(2026, 8, 25, 10, 0, 0)),
                new SaleDetailsPaymentDto(2, 2, 1, 120m, new DateTime(2026, 8, 25, 11, 0, 0))
            ]
        };

        var viewModel = new SaleDetailsDialogViewModel(
            details,
            new RecordingClipboardService(),
            new StubHistoryDialogService(),
            CancellationToken.None);

        Assert.Equal("მოქმედი გადახდები", viewModel.PaymentGroups[0].Header);
        Assert.Equal(2, Assert.Single(viewModel.PaymentGroups[0].Payments).CompletionVersion);
        Assert.Equal("წინა დასრულება #1", viewModel.PaymentGroups[1].Header);
        Assert.Equal(1, Assert.Single(viewModel.PaymentGroups[1].Payments).CompletionVersion);
    }

    [Fact]
    public void Print_AfterDebtPayment_UsesUpdatedCurrentFinancialSnapshot()
    {
        var addedPayment = new SaleDetailsPaymentDto(
            1,
            1,
            2,
            100m,
            new DateTime(2026, 8, 25, 12, 0, 0));
        var dialogs = new StubHistoryDialogService
        {
            DebtPaymentResult = new AddDebtPaymentResponse(
                1,
                200m,
                200m,
                0m,
                false,
                addedPayment)
        };
        var viewModel = new SaleDetailsDialogViewModel(
            CreateDetails("20260825-0019"),
            new RecordingClipboardService(),
            dialogs,
            CancellationToken.None);

        viewModel.PayDebtCommand.Execute(null);
        viewModel.PrintCommand.Execute(null);

        Assert.NotNull(dialogs.ReportedSale);
        Assert.Equal(200m, dialogs.ReportedSale.PaidAmount);
        Assert.Equal(0m, dialogs.ReportedSale.OutstandingAmount);
        Assert.False(dialogs.ReportedSale.HasDebt);
        Assert.Contains(addedPayment, dialogs.ReportedSale.Payments);
    }

    private static SaleDetailsDto CreateDetails(string saleNumber)
        => new(
            1,
            saleNumber,
            2,
            1,
            10,
            "Customer",
            null,
            null,
            200m,
            0m,
            200m,
            100m,
            100m,
            true,
            new DateTime(2026, 8, 25, 10, 0, 0),
            new DateTime(2026, 8, 25, 11, 0, 0),
            null,
            [],
            []);

    private sealed class RecordingClipboardService : IClipboardService
    {
        public string? Text { get; private set; }

        public bool TrySetText(string? text)
        {
            Text = text;
            return true;
        }
    }

    private sealed class StubHistoryDialogService : IHistoryDialogService
    {
        public AddDebtPaymentResponse? DebtPaymentResult { get; init; }
        public SaleDetailsDto? ReportedSale { get; private set; }

        public void ShowSaleReporting(SaleDetailsDto sale)
        {
            ReportedSale = sale;
        }

        public bool ShowSaleDetails(
            SaleDetailsDto sale,
            CancellationToken cancellationToken = default)
            => false;

        public AddDebtPaymentResponse? ShowDebtPayment(
            long saleId,
            decimal outstandingAmount,
            CancellationToken cancellationToken = default)
            => DebtPaymentResult;

        public bool ConfirmReopen(SalesHistoryItemDto sale) => false;
    }
}
