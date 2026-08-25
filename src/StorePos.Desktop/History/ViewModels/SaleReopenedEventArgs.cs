namespace StorePos.Desktop.History.ViewModels;

public sealed class SaleReopenedEventArgs(long saleId) : EventArgs
{
    public long SaleId { get; } = saleId;
}
