using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using StorePos.Desktop.Products.Models;
using StorePos.Desktop.Sales.ViewModels;

namespace StorePos.Desktop.Sales.Views;

public partial class SalesWorkspaceView : UserControl
{
    public SalesWorkspaceView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Unloaded += OnUnloaded;
    }

    private void OnDataContextChanged(
        object sender,
        DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is SalesWorkspaceViewModel oldViewModel)
        {
            oldViewModel.ProductNameFocusRequested -= OnProductNameFocusRequested;
            oldViewModel.ProductSearch.FocusRequested -= OnProductSearchFocusRequested;
        }

        if (e.NewValue is SalesWorkspaceViewModel newViewModel)
        {
            newViewModel.ProductNameFocusRequested += OnProductNameFocusRequested;
            newViewModel.ProductSearch.FocusRequested += OnProductSearchFocusRequested;
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SalesWorkspaceViewModel viewModel)
        {
            viewModel.ProductNameFocusRequested -= OnProductNameFocusRequested;
            viewModel.ProductSearch.FocusRequested -= OnProductSearchFocusRequested;
        }
    }

    private void OnProductSearchFocusRequested(object? sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(
            () =>
            {
                var searchTextBox = FindByTag<TextBox>(this, "CatalogProductSearch");
                searchTextBox?.Focus();
                searchTextBox?.SelectAll();
            },
            DispatcherPriority.Input);
    }

    private void OnProductNameFocusRequested(object? sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(
            () =>
            {
                var productNameTextBox = FindByTag<TextBox>(this, "ManualProductName");
                productNameTextBox?.Focus();
                productNameTextBox?.SelectAll();
            },
            DispatcherPriority.Input);
    }

    private void OnSaleItemsPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not SalesWorkspaceViewModel viewModel)
        {
            return;
        }

        if (e.Key == Key.Enter &&
            sender is DataGrid grid &&
            grid.CurrentColumn is { IsReadOnly: true })
        {
            var editCommand = viewModel.EditSelectedItemCommand;
            if (editCommand.CanExecute(null))
            {
                editCommand.Execute(null);
                e.Handled = true;
            }
            return;
        }

        var command = e.Key switch
        {
            Key.F2 => viewModel.EditSelectedItemCommand,
            Key.Delete => viewModel.RemoveSelectedItemCommand,
            _ => null
        };

        if (command?.CanExecute(null) != true)
        {
            return;
        }

        command.Execute(null);
        e.Handled = true;
    }

    private async void OnSaleItemCellEditEnding(
        object sender,
        DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction != DataGridEditAction.Commit ||
            e.Row.Item is not SaleItemViewModel item ||
            e.EditingElement is not TextBox editor ||
            DataContext is not SalesWorkspaceViewModel viewModel ||
            !TryGetInlineField(e.Column.SortMemberPath, out var field))
        {
            return;
        }

        await viewModel.UpdateItemFinancialsAsync(item, field, editor.Text);
    }

    private void OnSaleItemCurrentCellChanged(object? sender, EventArgs e)
    {
        if (sender is DataGrid { CurrentItem: SaleItemViewModel item } &&
            DataContext is SalesWorkspaceViewModel viewModel)
        {
            viewModel.SelectedItem = item;
        }
    }

    private void OnSaleItemSelectedCellsChanged(
        object sender,
        SelectedCellsChangedEventArgs e)
    {
        var item = e.AddedCells
            .Select(cell => cell.Item)
            .OfType<SaleItemViewModel>()
            .LastOrDefault();

        if (item is not null && DataContext is SalesWorkspaceViewModel viewModel)
        {
            viewModel.SelectedItem = item;
        }
    }

    private void OnSaleItemPreviewMouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        if (FindAncestor<DataGridRow>(e.OriginalSource as DependencyObject)
                is not { Item: SaleItemViewModel item } ||
            DataContext is not SalesWorkspaceViewModel viewModel)
        {
            return;
        }

        viewModel.SelectedItem = item;
    }

    private void OnDiscountLostKeyboardFocus(
        object sender,
        KeyboardFocusChangedEventArgs e)
    {
        if (DataContext is not SalesWorkspaceViewModel viewModel ||
            !viewModel.UpdateDiscountCommand.CanExecute(null))
        {
            return;
        }

        viewModel.UpdateDiscountCommand.Execute(null);
    }

    private void OnSaleItemCellDoubleClick(object sender, MouseButtonEventArgs e)
    {
        var cell = FindAncestor<DataGridCell>(e.OriginalSource as DependencyObject);
        if (cell is null || !cell.Column.IsReadOnly ||
            FindAncestor<DataGridRow>(cell) is not { Item: SaleItemViewModel item } ||
            DataContext is not SalesWorkspaceViewModel viewModel)
        {
            return;
        }

        viewModel.SelectedItem = item;
        if (viewModel.EditSelectedItemCommand.CanExecute(null))
        {
            viewModel.EditSelectedItemCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void OnProductSearchResultDoubleClick(object sender, MouseButtonEventArgs e)
    {
        var clickedItem = FindAncestor<ListBoxItem>(e.OriginalSource as DependencyObject);
        if (clickedItem?.DataContext is not ProductSearchResultDto product ||
            DataContext is not SalesWorkspaceViewModel viewModel)
        {
            return;
        }

        viewModel.ProductSearch.SelectedProduct = product;
        if (viewModel.ProductSearch.AddSelectedCommand.CanExecute(null) != true)
        {
            return;
        }

        viewModel.ProductSearch.AddSelectedCommand.Execute(null);
        e.Handled = true;
    }

    private void OnProductSearchSelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (sender is ListBox { SelectedItem: not null } listBox)
        {
            listBox.ScrollIntoView(listBox.SelectedItem);
        }
    }

    private static T? FindByTag<T>(DependencyObject parent, object tag)
        where T : FrameworkElement
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T element && Equals(element.Tag, tag))
            {
                return element;
            }

            var descendant = FindByTag<T>(child, tag);
            if (descendant is not null)
            {
                return descendant;
            }
        }

        return null;
    }

    private static T? FindAncestor<T>(DependencyObject? child)
        where T : DependencyObject
    {
        while (child is not null)
        {
            if (child is T match)
            {
                return match;
            }
            child = VisualTreeHelper.GetParent(child);
        }
        return null;
    }

    private static bool TryGetInlineField(
        string? sortMemberPath,
        out SaleItemInlineField field)
    {
        field = sortMemberPath switch
        {
            nameof(SaleItemViewModel.Quantity) => SaleItemInlineField.Quantity,
            nameof(SaleItemViewModel.UnitPrice) => SaleItemInlineField.UnitPrice,
            nameof(SaleItemViewModel.LineTotal) => SaleItemInlineField.LineTotal,
            _ => default
        };
        return sortMemberPath is nameof(SaleItemViewModel.Quantity) or
            nameof(SaleItemViewModel.UnitPrice) or
            nameof(SaleItemViewModel.LineTotal);
    }
}
