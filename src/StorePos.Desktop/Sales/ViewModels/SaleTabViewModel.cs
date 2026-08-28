using System.Collections.ObjectModel;
using StorePos.Desktop.Common;
using StorePos.Desktop.Sales.Models;
using StorePos.Desktop.Sales.Calculations;

namespace StorePos.Desktop.Sales.ViewModels;

public sealed class SaleTabViewModel : ObservableObject
{
    private decimal _subtotal;
    private decimal _discountAmount;
    private string _discountText;
    private decimal _totalAmount;
    private long? _customerId;
    private string? _customerName;
    private string? _customerIdentificationNumber;
    private string? _comment;
    private int _completionVersion;
    private decimal _paidAmount;
    private decimal _outstandingAmount;
    private bool _hasDebt;
    private PreviousCompletionPaymentStateDto? _previousCompletionPaymentState;

    public SaleTabViewModel(
        long id,
        string saleNumber,
        decimal subtotal,
        decimal discountAmount,
        decimal totalAmount,
        DateTime dateCreated,
        long? customerId,
        string? customerName,
        string? customerIdentificationNumber = null,
        string? comment = null,
        bool isDetailsLoaded = false)
    {
        Id = id;
        SaleNumber = saleNumber;
        _subtotal = subtotal;
        _discountAmount = discountAmount;
        _discountText = DecimalInputParser.Format(discountAmount);
        _totalAmount = totalAmount;
        DateCreated = dateCreated;
        _customerId = customerId;
        _customerName = customerName;
        _customerIdentificationNumber = customerIdentificationNumber;
        _comment = comment;
        IsDetailsLoaded = isDetailsLoaded;
    }

    public long Id { get; }

    public string SaleNumber { get; }

    public decimal Subtotal
    {
        get => _subtotal;
        private set => SetProperty(ref _subtotal, value);
    }

    public decimal DiscountAmount
    {
        get => _discountAmount;
        private set => SetProperty(ref _discountAmount, value);
    }

    public string DiscountText
    {
        get => _discountText;
        set => SetProperty(ref _discountText, value);
    }

    public decimal TotalAmount
    {
        get => _totalAmount;
        private set => SetProperty(ref _totalAmount, value);
    }

    public DateTime DateCreated { get; }

    public long? CustomerId
    {
        get => _customerId;
        private set
        {
            if (SetProperty(ref _customerId, value))
            {
                NotifyCustomerState();
            }
        }
    }

    public string? CustomerName
    {
        get => _customerName;
        set
        {
            if (SetProperty(ref _customerName, value))
            {
                NotifyCustomerState();
            }
        }
    }

    public string? CustomerIdentificationNumber
    {
        get => _customerIdentificationNumber;
        set => SetProperty(ref _customerIdentificationNumber, value);
    }

    public string? Comment
    {
        get => _comment;
        set => SetProperty(ref _comment, value);
    }

    public bool IsDetailsLoaded { get; private set; }

    public int CompletionVersion
    {
        get => _completionVersion;
        private set => SetProperty(ref _completionVersion, value);
    }

    public decimal PaidAmount
    {
        get => _paidAmount;
        private set => SetProperty(ref _paidAmount, value);
    }

    public decimal OutstandingAmount
    {
        get => _outstandingAmount;
        private set => SetProperty(ref _outstandingAmount, value);
    }

    public bool HasDebt
    {
        get => _hasDebt;
        private set => SetProperty(ref _hasDebt, value);
    }

    public bool HasAssignedCustomer =>
        CustomerId.HasValue || !string.IsNullOrWhiteSpace(CustomerName);

    public string CustomerStatusText => HasAssignedCustomer
        ? CustomerName ?? "მყიდველი მითითებულია"
        : "მყიდველი არ არის მითითებული";

    public PreviousCompletionPaymentStateDto? PreviousCompletionPaymentState
    {
        get => _previousCompletionPaymentState;
        private set => SetProperty(ref _previousCompletionPaymentState, value);
    }

    public ObservableCollection<SaleItemViewModel> Items { get; } = [];

    public void ApplyDetails(
        decimal subtotal,
        decimal discountAmount,
        decimal totalAmount,
        long? customerId,
        string? customerName,
        string? customerIdentificationNumber,
        string? comment,
        IEnumerable<SaleItemViewModel> items,
        int completionVersion,
        decimal paidAmount,
        decimal outstandingAmount,
        bool hasDebt,
        PreviousCompletionPaymentStateDto? previousCompletionPaymentState)
    {
        Items.Clear();
        foreach (var item in items)
        {
            Items.Add(item);
        }

        ApplyFinancials(subtotal, discountAmount, totalAmount);
        CustomerId = customerId;
        CustomerName = customerName;
        CustomerIdentificationNumber = customerIdentificationNumber;
        Comment = comment;
        CompletionVersion = completionVersion;
        PaidAmount = paidAmount;
        OutstandingAmount = outstandingAmount;
        HasDebt = hasDebt;
        PreviousCompletionPaymentState = previousCompletionPaymentState;
        IsDetailsLoaded = true;
    }

    public void ApplyCustomerInfo(
        long? customerId,
        string? customerName,
        string? customerIdentificationNumber,
        string? comment)
    {
        CustomerId = customerId;
        CustomerName = customerName;
        CustomerIdentificationNumber = customerIdentificationNumber;
        Comment = comment;
    }

    public void AddItem(SaleItemViewModel item, decimal totalAmount)
    {
        Items.Add(item);
        ApplyTotalAmount(totalAmount);
        IsDetailsLoaded = true;
    }

    public void ApplyCatalogItem(SaleItemViewModel item, bool wasNewItem, decimal totalAmount)
    {
        if (wasNewItem)
        {
            Items.Add(item);
        }
        else
        {
            var existingItem = Items.Single(existing => existing.Id == item.Id);
            existingItem.ApplyUpdate(
                existingItem.ProductName,
                item.Quantity,
                item.UnitPrice,
                item.LineTotal,
                item.Comment);
        }

        ApplyTotalAmount(totalAmount);
        IsDetailsLoaded = true;
    }

    public void ApplyItemUpdate(
        long saleItemId,
        string productName,
        decimal quantity,
        decimal unitPrice,
        decimal lineTotal,
        string? comment,
        decimal totalAmount)
    {
        var item = Items.Single(existingItem => existingItem.Id == saleItemId);
        item.ApplyUpdate(productName, quantity, unitPrice, lineTotal, comment);
        ApplyTotalAmount(totalAmount);
    }

    public void ApplyItemRemoval(long saleItemId, decimal totalAmount)
    {
        var item = Items.Single(existingItem => existingItem.Id == saleItemId);
        Items.Remove(item);
        ApplyTotalAmount(totalAmount);
    }

    public void ApplyFinancials(
        decimal subtotal,
        decimal discountAmount,
        decimal totalAmount)
    {
        Subtotal = subtotal;
        DiscountAmount = discountAmount;
        DiscountText = DecimalInputParser.Format(discountAmount);
        ApplyTotalAmount(totalAmount);
    }

    private void ApplyTotalAmount(decimal totalAmount)
    {
        TotalAmount = totalAmount;
        Subtotal = FinancialInputPrecision.RoundMoney(TotalAmount + DiscountAmount);
        if (CompletionVersion == 0)
        {
            PaidAmount = 0m;
            OutstandingAmount = 0m;
            HasDebt = false;
            return;
        }

        OutstandingAmount = Math.Max(
            FinancialInputPrecision.RoundMoney(TotalAmount - PaidAmount),
            0m);
        HasDebt = OutstandingAmount > 0;
    }

    private void NotifyCustomerState()
    {
        OnPropertyChanged(nameof(HasAssignedCustomer));
        OnPropertyChanged(nameof(CustomerStatusText));
    }
}
