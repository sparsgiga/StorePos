namespace StorePos.Desktop.Products.ViewModels;

public sealed class ProductSearchErrorEventArgs(string message, Exception? exception = null)
    : EventArgs
{
    public string Message { get; } = message;

    public Exception? Exception { get; } = exception;
}
