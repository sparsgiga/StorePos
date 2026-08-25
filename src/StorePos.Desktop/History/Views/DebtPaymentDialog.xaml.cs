using System.Windows;
using StorePos.Desktop.History.ViewModels;
using StorePos.Desktop.Sales.Dialogs;

namespace StorePos.Desktop.History.Views;

public partial class DebtPaymentDialog : Window
{
    private readonly DebtPaymentDialogViewModel _viewModel;

    public DebtPaymentDialog(DebtPaymentDialogViewModel viewModel)
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
