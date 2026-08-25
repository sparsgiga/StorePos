namespace StorePos.Desktop.Sales.Dialogs;

public sealed class DialogCloseRequestedEventArgs(bool dialogResult) : EventArgs
{
    public bool DialogResult { get; } = dialogResult;
}
