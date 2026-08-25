using System.Collections.ObjectModel;
using StorePos.Desktop.Common;

namespace StorePos.Desktop.Sales.ViewModels;

public sealed class SaleTabViewModel : ObservableObject
{
    private decimal _totalAmount;
    private long? _customerId;
    private string? _customerName;
    private string? _customerIdentificationNumber;
    private string? _comment;

    public SaleTabViewModel(
        long id,
        string saleNumber,
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

    public decimal TotalAmount
    {
        get => _totalAmount;
        private set => SetProperty(ref _totalAmount, value);
    }

    public DateTime DateCreated { get; }

    public long? CustomerId
    {
        get => _customerId;
        private set => SetProperty(ref _customerId, value);
    }

    public string? CustomerName
    {
        get => _customerName;
        set => SetProperty(ref _customerName, value);
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

    public ObservableCollection<SaleItemViewModel> Items { get; } = [];

    public void ApplyDetails(
        decimal totalAmount,
        long? customerId,
        string? customerName,
        string? customerIdentificationNumber,
        string? comment,
        IEnumerable<SaleItemViewModel> items)
    {
        Items.Clear();
        foreach (var item in items)
        {
            Items.Add(item);
        }

        TotalAmount = totalAmount;
        CustomerId = customerId;
        CustomerName = customerName;
        CustomerIdentificationNumber = customerIdentificationNumber;
        Comment = comment;
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
        TotalAmount = totalAmount;
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

        TotalAmount = totalAmount;
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
        TotalAmount = totalAmount;
    }

    public void ApplyItemRemoval(long saleItemId, decimal totalAmount)
    {
        var item = Items.Single(existingItem => existingItem.Id == saleItemId);
        Items.Remove(item);
        TotalAmount = totalAmount;
    }
}
