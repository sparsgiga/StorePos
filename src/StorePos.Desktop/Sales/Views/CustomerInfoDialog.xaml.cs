using System.Windows;
using StorePos.Desktop.Sales.Dialogs;
using StorePos.Desktop.Sales.ViewModels;

namespace StorePos.Desktop.Sales.Views;

public partial class CustomerInfoDialog : Window
{
    private readonly CustomerInfoDialogViewModel _viewModel;

    public CustomerInfoDialog(CustomerInfoDialogViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        viewModel.CloseRequested += OnCloseRequested;
        Closed += OnClosed;
    }

    private void OnCloseRequested(
        object? sender,
        DialogCloseRequestedEventArgs e)
        => DialogResult = e.DialogResult;

    private void OnClosed(object? sender, EventArgs e)
        => _viewModel.CloseRequested -= OnCloseRequested;
}
