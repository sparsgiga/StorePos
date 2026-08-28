using System.Windows;
using System.Windows.Input;
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
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        SearchBox.Focus();
        await _viewModel.InitializeAsync();
    }

    private void OnCloseRequested(
        object? sender,
        DialogCloseRequestedEventArgs e)
        => DialogResult = e.DialogResult;

    private void OnClosed(object? sender, EventArgs e)
    {
        Loaded -= OnLoaded;
        _viewModel.CloseRequested -= OnCloseRequested;
        _viewModel.Dispose();
    }

    private void OnCustomerDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel.SelectCustomerCommand.CanExecute(null))
        {
            _viewModel.SelectCustomerCommand.Execute(null);
            e.Handled = true;
        }
    }
}
