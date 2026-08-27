using System.Windows;
using StorePos.Desktop.Products.ViewModels;
using StorePos.Desktop.Sales.Dialogs;

namespace StorePos.Desktop.Products.Views;

public partial class QuickRetailPriceDialog : Window
{
    private readonly QuickRetailPriceDialogViewModel _viewModel;

    public QuickRetailPriceDialog(QuickRetailPriceDialogViewModel viewModel)
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
