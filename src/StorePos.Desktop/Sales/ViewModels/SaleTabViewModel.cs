using System.Collections.ObjectModel;
using StorePos.Desktop.Common;

namespace StorePos.Desktop.Sales.ViewModels;

public sealed class SaleTabViewModel : ObservableObject
{
    private decimal _totalAmount;

    public SaleTabViewModel(
        long id,
        string saleNumber,
        decimal totalAmount,
        DateTime dateCreated,
        string? customerName,
        bool isDetailsLoaded = false)
    {
        Id = id;
        SaleNumber = saleNumber;
        _totalAmount = totalAmount;
        DateCreated = dateCreated;
        CustomerName = customerName;
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

    public string? CustomerName { get; }

    public bool IsDetailsLoaded { get; private set; }

    public ObservableCollection<SaleItemViewModel> Items { get; } = [];

    public void ApplyDetails(
        decimal totalAmount,
        IEnumerable<SaleItemViewModel> items)
    {
        Items.Clear();
        foreach (var item in items)
        {
            Items.Add(item);
        }

        TotalAmount = totalAmount;
        IsDetailsLoaded = true;
    }

    public void AddItem(SaleItemViewModel item, decimal totalAmount)
    {
        Items.Add(item);
        TotalAmount = totalAmount;
        IsDetailsLoaded = true;
    }
}
