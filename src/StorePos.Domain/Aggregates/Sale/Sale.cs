using StorePos.Domain.Base;
using StorePos.Domain.Enums;

namespace StorePos.Domain.Aggregates.Sale;

public sealed class Sale : AuditableEntity<long>, IAggregateRoot
{
    private readonly List<SaleItem> _items = [];
    private readonly List<SalePayment> _payments = [];

    private Sale()
    {
    }

    private Sale(
        string saleNumber,
        long? cashierId,
        string? customerName,
        string? customerIdentificationNumber,
        string? note)
    {
        SaleNumber = saleNumber;
        CashierId = cashierId;
        CustomerName = customerName;
        CustomerIdentificationNumber = customerIdentificationNumber;
        Comment = note;
        Status = SaleStatus.Draft;
    }

    public string SaleNumber { get; private set; } = string.Empty;

    public SaleStatus Status { get; private set; }

    public long? CashierId { get; private set; }

    public string? CustomerName { get; private set; }

    public string? CustomerIdentificationNumber { get; private set; }

    public decimal TotalAmount { get; private set; }

    public string? Comment { get; private set; }

    public DateTime? DateCompleted { get; private set; }

    public DateTime? DateCancelled { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<SaleItem> Items => _items;

    public IReadOnlyCollection<SalePayment> Payments => _payments;

    public static Sale Create(
        string saleNumber,
        long? cashierId = null,
        string? customerName = null,
        string? customerIdentificationNumber = null,
        string? note = null)
        => new(saleNumber, cashierId, customerName, customerIdentificationNumber, note);

    public SaleItem AddManualItem(
        string productName,
        decimal quantity,
        decimal unitPrice,
        string? comment = null)
    {
        EnsureDraft();

        var item = SaleItem.CreateManual(
            Id,
            productName,
            quantity,
            unitPrice,
            comment);

        _items.Add(item);
        RecalculateTotal();

        return item;
    }

    public void Complete(DateTime dateCompleted)
    {
        EnsureDraft();
        Status = SaleStatus.Completed;
        DateCompleted = dateCompleted;
    }

    public void Cancel(DateTime dateCancelled)
    {
        EnsureDraft();
        Status = SaleStatus.Cancelled;
        DateCancelled = dateCancelled;
    }

    private void EnsureDraft()
    {
        if (Status != SaleStatus.Draft)
        {
            throw new InvalidOperationException("Only a draft sale can be modified.");
        }
    }

    private void RecalculateTotal()
        => TotalAmount = _items.Sum(item => item.LineTotal);
}
