namespace StorePos.Desktop.Products.Dialogs;

public interface IProductDialogService
{
    Task<bool> ShowCreateAsync(CancellationToken cancellationToken = default);

    Task<bool> ShowEditAsync(long productId, CancellationToken cancellationToken = default);

    bool ConfirmDeactivate(string productName);
}
