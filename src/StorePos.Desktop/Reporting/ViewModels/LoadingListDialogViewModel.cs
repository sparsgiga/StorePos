using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using StorePos.Desktop.Common;
using StorePos.Desktop.Reporting.Models;

namespace StorePos.Desktop.Reporting.ViewModels;

public sealed class LoadingListDialogViewModel : ObservableObject
{
    private readonly FullSaleReportModel _sale;
    private readonly Func<LoadingListReportModel, Task> _preview;
    private readonly TimeProvider _timeProvider;
    private readonly AsyncRelayCommand _previewCommand;
    private readonly RelayCommand _selectAllCommand;
    private readonly RelayCommand _clearAllCommand;
    private string? _printComment;
    private bool _isBusy;
    private string? _errorMessage;

    public LoadingListDialogViewModel(
        FullSaleReportModel sale,
        Func<LoadingListReportModel, Task> preview,
        TimeProvider? timeProvider = null)
    {
        _sale = sale;
        _preview = preview;
        _timeProvider = timeProvider ?? TimeProvider.System;
        Items = new ObservableCollection<LoadingListItemSelectionViewModel>(
            sale.Items.Select(item => new LoadingListItemSelectionViewModel(item)));
        foreach (var item in Items)
        {
            item.PropertyChanged += OnItemPropertyChanged;
        }

        _previewCommand = new AsyncRelayCommand(PreviewAsync, CanPreview);
        _selectAllCommand = new RelayCommand(() => SetSelection(true), () => !IsBusy);
        _clearAllCommand = new RelayCommand(() => SetSelection(false), () => !IsBusy);
    }

    public string SaleNumber => _sale.SaleNumber;
    public string? CustomerName => _sale.CustomerName;
    public ObservableCollection<LoadingListItemSelectionViewModel> Items { get; }

    public string? PrintComment
    {
        get => _printComment;
        set => SetProperty(ref _printComment, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                NotifyCommandStates();
            }
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public bool CanCreateReport =>
        Items.Any(item => item.IsSelected) &&
        Items.Where(item => item.IsSelected).All(item => item.IsValid);

    public ICommand PreviewCommand => _previewCommand;
    public ICommand SelectAllCommand => _selectAllCommand;
    public ICommand ClearAllCommand => _clearAllCommand;

    public LoadingListReportModel BuildReport(DateTime printedAt)
    {
        if (!CanCreateReport)
        {
            throw new InvalidOperationException(
                "At least one valid loading-list item must be selected.");
        }

        return new LoadingListReportModel(
            _sale.SaleId,
            _sale.SaleNumber,
            _sale.Status,
            _sale.CustomerName,
            printedAt,
            string.IsNullOrWhiteSpace(PrintComment) ? null : PrintComment.Trim(),
            Items
                .Where(item => item.IsSelected)
                .Select(item => item.CreateReportItem())
                .ToArray());
    }

    private async Task PreviewAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            await Task.Yield();
            await _preview(BuildReport(_timeProvider.GetLocalNow().DateTime));
        }
        catch (Exception exception)
        {
            ErrorMessage = $"დოკუმენტის მომზადება ვერ მოხერხდა: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanPreview() => !IsBusy && CanCreateReport;

    private void SetSelection(bool isSelected)
    {
        foreach (var item in Items)
        {
            item.IsSelected = isSelected;
        }
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(LoadingListItemSelectionViewModel.IsSelected) or
            nameof(LoadingListItemSelectionViewModel.IsValid))
        {
            OnPropertyChanged(nameof(CanCreateReport));
            _previewCommand.NotifyCanExecuteChanged();
        }
    }

    private void NotifyCommandStates()
    {
        _previewCommand.NotifyCanExecuteChanged();
        _selectAllCommand.NotifyCanExecuteChanged();
        _clearAllCommand.NotifyCanExecuteChanged();
    }
}
