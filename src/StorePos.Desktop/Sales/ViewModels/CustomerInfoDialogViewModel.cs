using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using StorePos.Desktop.Api;
using StorePos.Desktop.Common;
using StorePos.Desktop.Customers;
using StorePos.Desktop.Customers.Models;
using StorePos.Desktop.Sales.Dialogs;
using StorePos.Desktop.Sales.Models;

namespace StorePos.Desktop.Sales.ViewModels;

public sealed class CustomerInfoDialogViewModel : ObservableObject, IDisposable
{
    private static readonly TimeSpan SearchDelay = TimeSpan.FromMilliseconds(275);
    private readonly IStorePosApiClient _apiClient;
    private readonly long _saleId;
    private readonly CancellationTokenSource _dialogCancellation;
    private readonly CancellationToken _lifetimeCancellation;
    private readonly AsyncRelayCommand _selectCustomerCommand;
    private readonly AsyncRelayCommand _saveCustomerCommand;
    private readonly AsyncRelayCommand _removeCustomerCommand;
    private readonly AsyncRelayCommand _saveSaleCommentCommand;
    private readonly RelayCommand _editCustomerCommand;
    private CancellationTokenSource? _searchCancellation;
    private long? _customerId;
    private string? _customerName;
    private string? _customerIdentificationNumber;
    private string? _assignedCustomerInformation;
    private string? _saleComment;
    private string? _searchText;
    private CustomerDto? _selectedCustomer;
    private bool _isEditorVisible;
    private bool _isEditingCustomer;
    private long? _editingCustomerId;
    private string? _editorName;
    private string? _editorIdentificationNumber;
    private string? _editorInformation;
    private string? _errorMessage;
    private string? _statusMessage;
    private bool _isBusy;

    public CustomerInfoDialogViewModel(
        IStorePosApiClient apiClient,
        long saleId,
        long? customerId,
        string? customerName,
        string? customerIdentificationNumber,
        string? saleComment,
        CancellationToken cancellationToken)
    {
        _apiClient = apiClient;
        _saleId = saleId;
        _customerId = customerId;
        _customerName = customerName;
        _customerIdentificationNumber = customerIdentificationNumber;
        _saleComment = saleComment;
        _dialogCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _lifetimeCancellation = _dialogCancellation.Token;

        Result = CreateResult();
        _selectCustomerCommand = new AsyncRelayCommand(
            SelectCustomerAsync,
            () => SelectedCustomer is not null && !IsBusy);
        _saveCustomerCommand = new AsyncRelayCommand(
            SaveCustomerAsync,
            () => !string.IsNullOrWhiteSpace(EditorName) && !IsBusy);
        _removeCustomerCommand = new AsyncRelayCommand(
            RemoveCustomerAsync,
            () => HasCustomerSnapshot && !IsBusy);
        _saveSaleCommentCommand = new AsyncRelayCommand(
            SaveSaleCommentAsync,
            () => !IsBusy);
        _editCustomerCommand = new RelayCommand(
            BeginEditCustomer,
            () => SelectedCustomer is not null && !IsBusy);

        NewCustomerCommand = new RelayCommand(BeginCreateCustomer, () => !IsBusy);
        BackToSearchCommand = new RelayCommand(ShowSearch, () => !IsBusy);
        CloseCommand = new RelayCommand(
            () => CloseRequested?.Invoke(this, new DialogCloseRequestedEventArgs(true)));
    }

    public event EventHandler<DialogCloseRequestedEventArgs>? CloseRequested;

    public ObservableCollection<CustomerDto> SearchResults { get; } = [];

    public long? CustomerId
    {
        get => _customerId;
        private set
        {
            if (SetProperty(ref _customerId, value))
            {
                OnPropertyChanged(nameof(HasCustomerSnapshot));
                OnPropertyChanged(nameof(IsLegacySnapshot));
                _removeCustomerCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string? CustomerName
    {
        get => _customerName;
        private set
        {
            if (SetProperty(ref _customerName, value))
            {
                OnPropertyChanged(nameof(HasCustomerSnapshot));
                OnPropertyChanged(nameof(IsLegacySnapshot));
                _removeCustomerCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string? CustomerIdentificationNumber
    {
        get => _customerIdentificationNumber;
        private set
        {
            if (SetProperty(ref _customerIdentificationNumber, value))
            {
                OnPropertyChanged(nameof(HasCustomerSnapshot));
                OnPropertyChanged(nameof(IsLegacySnapshot));
                _removeCustomerCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string? AssignedCustomerInformation
    {
        get => _assignedCustomerInformation;
        private set => SetProperty(ref _assignedCustomerInformation, value);
    }

    public string? SaleComment
    {
        get => _saleComment;
        set => SetProperty(ref _saleComment, value);
    }

    public bool HasCustomerSnapshot =>
        CustomerId.HasValue ||
        !string.IsNullOrWhiteSpace(CustomerName) ||
        !string.IsNullOrWhiteSpace(CustomerIdentificationNumber);

    public bool IsLegacySnapshot => !CustomerId.HasValue && HasCustomerSnapshot;

    public string? SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                _ = DebounceSearchAsync(value);
            }
        }
    }

    public CustomerDto? SelectedCustomer
    {
        get => _selectedCustomer;
        set
        {
            if (SetProperty(ref _selectedCustomer, value))
            {
                _selectCustomerCommand.NotifyCanExecuteChanged();
                _editCustomerCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool IsEditorVisible
    {
        get => _isEditorVisible;
        private set
        {
            if (SetProperty(ref _isEditorVisible, value))
            {
                OnPropertyChanged(nameof(IsSearchVisible));
            }
        }
    }

    public bool IsSearchVisible => !IsEditorVisible;

    public bool IsEditingCustomer
    {
        get => _isEditingCustomer;
        private set
        {
            if (SetProperty(ref _isEditingCustomer, value))
            {
                OnPropertyChanged(nameof(EditorTitle));
                OnPropertyChanged(nameof(EditorSaveText));
            }
        }
    }

    public string EditorTitle => IsEditingCustomer
        ? "მყიდველის რედაქტირება"
        : "ახალი მყიდველი";

    public string EditorSaveText => IsEditingCustomer ? "შენახვა" : "დამატება";

    public string? EditorName
    {
        get => _editorName;
        set
        {
            if (SetProperty(ref _editorName, value))
            {
                _saveCustomerCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string? EditorIdentificationNumber
    {
        get => _editorIdentificationNumber;
        set => SetProperty(ref _editorIdentificationNumber, value);
    }

    public string? EditorInformation
    {
        get => _editorInformation;
        set => SetProperty(ref _editorInformation, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public string? StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
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

    public CustomerDialogResult Result { get; private set; }

    public ICommand SelectCustomerCommand => _selectCustomerCommand;
    public ICommand EditCustomerCommand => _editCustomerCommand;
    public ICommand NewCustomerCommand { get; }
    public ICommand SaveCustomerCommand => _saveCustomerCommand;
    public ICommand BackToSearchCommand { get; }
    public ICommand RemoveCustomerCommand => _removeCustomerCommand;
    public ICommand SaveSaleCommentCommand => _saveSaleCommentCommand;
    public ICommand CloseCommand { get; }

    public async Task InitializeAsync()
    {
        if (!CustomerId.HasValue)
        {
            return;
        }

        try
        {
            var customer = await _apiClient.GetCustomerAsync(
                CustomerId.Value,
                _lifetimeCancellation);
            AssignedCustomerInformation = customer.Information;
            SelectedCustomer = customer;
        }
        catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            Trace.TraceError(exception.ToString());
            ErrorMessage = "მიბმული მყიდველის მიმდინარე ინფორმაციის ჩატვირთვა ვერ მოხერხდა.";
        }
    }

    public void Dispose()
    {
        _dialogCancellation.Cancel();
        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();
        _dialogCancellation.Dispose();
    }

    private async Task DebounceSearchAsync(string? value)
    {
        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();

        var query = value?.Trim();
        if (string.IsNullOrEmpty(query) || query.Length < 2)
        {
            SearchResults.Clear();
            return;
        }

        var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
            _lifetimeCancellation);
        _searchCancellation = cancellation;

        try
        {
            await Task.Delay(SearchDelay, cancellation.Token);
            var results = await _apiClient.SearchCustomersAsync(
                query,
                cancellationToken: cancellation.Token);

            if (!ReferenceEquals(_searchCancellation, cancellation))
            {
                return;
            }

            SearchResults.Clear();
            foreach (var customer in results)
            {
                SearchResults.Add(customer);
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            Trace.TraceError(exception.ToString());
            ErrorMessage = "მყიდველების ძებნა ვერ მოხერხდა.";
        }
        finally
        {
            if (ReferenceEquals(_searchCancellation, cancellation))
            {
                _searchCancellation = null;
                cancellation.Dispose();
            }
        }
    }

    private async Task SelectCustomerAsync()
    {
        var selected = SelectedCustomer;
        if (selected is null)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ClearMessages();
            var response = await _apiClient.AssignCustomerToSaleAsync(
                _saleId,
                selected.Id,
                _lifetimeCancellation);
            ApplySaleResponse(response);
            CloseRequested?.Invoke(this, new DialogCloseRequestedEventArgs(true));
        }
        catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            HandleError(exception, "მყიდველის გაყიდვაზე მიბმა ვერ მოხერხდა.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void BeginCreateCustomer()
    {
        IsEditingCustomer = false;
        _editingCustomerId = null;
        EditorName = SearchText;
        EditorIdentificationNumber = null;
        EditorInformation = null;
        ClearMessages();
        IsEditorVisible = true;
    }

    private void BeginEditCustomer()
    {
        var selected = SelectedCustomer;
        if (selected is null)
        {
            return;
        }

        IsEditingCustomer = true;
        _editingCustomerId = selected.Id;
        EditorName = selected.Name;
        EditorIdentificationNumber = selected.IdentificationNumber;
        EditorInformation = selected.Information;
        ClearMessages();
        IsEditorVisible = true;
    }

    private async Task SaveCustomerAsync()
    {
        if (string.IsNullOrWhiteSpace(EditorName))
        {
            return;
        }

        try
        {
            IsBusy = true;
            ClearMessages();

            if (IsEditingCustomer && _editingCustomerId.HasValue)
            {
                var updated = await _apiClient.UpdateCustomerAsync(
                    _editingCustomerId.Value,
                    new UpdateCustomerRequest(
                        EditorName,
                        EditorIdentificationNumber,
                        EditorInformation),
                    _lifetimeCancellation);
                ReplaceSearchResult(updated);
                SelectedCustomer = updated;
                if (CustomerId == updated.Id)
                {
                    AssignedCustomerInformation = updated.Information;
                }
                IsEditorVisible = false;
                StatusMessage = "მყიდველის ინფორმაცია შენახულია. გაყიდვის snapshot-ის განახლებისთვის ხელახლა აირჩიეთ მყიდველი.";
                return;
            }

            var created = await _apiClient.CreateCustomerAsync(
                new CreateCustomerRequest(
                    EditorName,
                    EditorIdentificationNumber,
                    EditorInformation),
                _lifetimeCancellation);
            var response = await _apiClient.AssignCustomerToSaleAsync(
                _saleId,
                created.Id,
                _lifetimeCancellation);
            ApplySaleResponse(response);
            CloseRequested?.Invoke(this, new DialogCloseRequestedEventArgs(true));
        }
        catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
        {
        }
        catch (CustomerConflictException exception)
        {
            ErrorMessage = exception.Message;
        }
        catch (Exception exception)
        {
            HandleError(exception, "მყიდველის ინფორმაციის შენახვა ვერ მოხერხდა.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RemoveCustomerAsync()
    {
        try
        {
            IsBusy = true;
            ClearMessages();
            var response = await _apiClient.RemoveCustomerFromSaleAsync(
                _saleId,
                _lifetimeCancellation);
            AssignedCustomerInformation = null;
            ApplySaleResponse(response);
            CloseRequested?.Invoke(this, new DialogCloseRequestedEventArgs(true));
        }
        catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            HandleError(exception, "მყიდველის გაყიდვიდან მოხსნა ვერ მოხერხდა.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SaveSaleCommentAsync()
    {
        try
        {
            IsBusy = true;
            ClearMessages();
            var response = await _apiClient.UpdateSaleCommentAsync(
                _saleId,
                SaleComment,
                _lifetimeCancellation);
            SaleComment = response.Comment;
            Result = CreateResult();
            StatusMessage = "გაყიდვის კომენტარი შენახულია.";
        }
        catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            HandleError(exception, "გაყიდვის კომენტარის შენახვა ვერ მოხერხდა.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ShowSearch()
    {
        IsEditorVisible = false;
        ClearMessages();
    }

    private void ApplySaleResponse(SaleCustomerResponse response)
    {
        CustomerId = response.CustomerId;
        CustomerName = response.CustomerName;
        CustomerIdentificationNumber = response.CustomerIdentificationNumber;
        SaleComment = response.SaleComment;
        Result = CreateResult();
    }

    private CustomerDialogResult CreateResult()
        => new(CustomerId, CustomerName, CustomerIdentificationNumber, SaleComment);

    private void ReplaceSearchResult(CustomerDto updated)
    {
        var existing = SearchResults.FirstOrDefault(customer => customer.Id == updated.Id);
        if (existing is null)
        {
            SearchResults.Insert(0, updated);
            return;
        }

        SearchResults[SearchResults.IndexOf(existing)] = updated;
    }

    private void ClearMessages()
    {
        ErrorMessage = null;
        StatusMessage = null;
    }

    private void HandleError(Exception exception, string message)
    {
        Trace.TraceError(exception.ToString());
        ErrorMessage = message;
    }

    private void NotifyCommandStates()
    {
        _selectCustomerCommand.NotifyCanExecuteChanged();
        _saveCustomerCommand.NotifyCanExecuteChanged();
        _removeCustomerCommand.NotifyCanExecuteChanged();
        _saveSaleCommentCommand.NotifyCanExecuteChanged();
        _editCustomerCommand.NotifyCanExecuteChanged();
    }
}
