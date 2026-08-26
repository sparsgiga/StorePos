using System.Windows;
using StorePos.Desktop.Api;
using StorePos.Desktop.Products.ViewModels;
using StorePos.Desktop.Products.Views;

namespace StorePos.Desktop.Products.Dialogs;

public sealed class ProductDialogService(IStorePosApiClient apiClient)
    : IProductDialogService
{
    public Task<bool> ShowCreateAsync(CancellationToken cancellationToken = default)
        => ShowEditorAsync(null, cancellationToken);

    public Task<bool> ShowEditAsync(
        long productId,
        CancellationToken cancellationToken = default)
        => ShowEditorAsync(productId, cancellationToken);

    public bool ConfirmDeactivate(string productName)
        => MessageBox.Show(
               Application.Current.MainWindow,
               $"„{productName}“-ის დეაქტივაციის შემდეგ იგი ახალ გაყიდვებში აღარ გამოჩნდება.",
               "პროდუქტის დეაქტივაცია",
               MessageBoxButton.YesNo,
               MessageBoxImage.Warning,
               MessageBoxResult.No) == MessageBoxResult.Yes;

    private async Task<bool> ShowEditorAsync(
        long? productId,
        CancellationToken cancellationToken)
    {
        var viewModel = new ProductEditorDialogViewModel(
            apiClient,
            productId,
            cancellationToken);
        await viewModel.InitializeAsync();
        var dialog = new ProductEditorDialog(viewModel)
        {
            Owner = Application.Current.MainWindow
        };
        return dialog.ShowDialog() == true;
    }
}
