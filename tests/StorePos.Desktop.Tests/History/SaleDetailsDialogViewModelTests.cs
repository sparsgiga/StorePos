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

    private static SaleDetailsDto CreateDetails(string saleNumber)
        => new(
            1,
            saleNumber,
            2,
            10,
            "Customer",
            null,
            null,
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
        public bool ShowSaleDetails(
            SaleDetailsDto sale,
            CancellationToken cancellationToken = default)
            => false;

        public AddDebtPaymentResponse? ShowDebtPayment(
            long saleId,
            decimal outstandingAmount,
            CancellationToken cancellationToken = default)
            => null;

        public bool ConfirmReopen(SalesHistoryItemDto sale) => false;
    }
}
