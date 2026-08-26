using System.Windows;
using StorePos.Desktop.Products.ViewModels;
using StorePos.Desktop.Sales.Dialogs;

namespace StorePos.Desktop.Products.Views;

public partial class ProductEditorDialog : Window
{
    private readonly ProductEditorDialogViewModel _viewModel;

    public ProductEditorDialog(ProductEditorDialogViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        viewModel.CloseRequested += OnCloseRequested;
        Closed += OnClosed;
    }

    private void OnCloseRequested(object? sender, DialogCloseRequestedEventArgs e)
        => DialogResult = e.DialogResult;

    private void OnClosed(object? sender, EventArgs e)
        => _viewModel.CloseRequested -= OnCloseRequested;
}
