namespace StorePos.Desktop.Products.ViewModels;

public sealed class ManualProductFallbackEventArgs(string value, bool isBarcode) : EventArgs
{
    public string Value { get; } = value;

    public bool IsBarcode { get; } = isBarcode;
}
