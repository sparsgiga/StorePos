using StorePos.Domain.Base;
using StorePos.Domain.Enums;

namespace StorePos.Domain.Aggregates.Sale;

public sealed class Sale : AuditableEntity<long>, IAggregateRoot
{
    public const int CustomerNameMaxLength = 300;
    public const int CustomerIdentificationNumberMaxLength = 50;
    public const int CommentMaxLength = 4000;

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
        string? comment)
    {
        SaleNumber = saleNumber;
        CashierId = cashierId;
        Status = SaleStatus.Draft;
        UpdateInfo(customerName, customerIdentificationNumber, comment);
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
        string? comment = null)
        => new(saleNumber, cashierId, customerName, customerIdentificationNumber, comment);

    public void UpdateInfo(
        string? customerName,
        string? customerIdentificationNumber,
        string? comment)
    {
        EnsureDraft();

        CustomerName = NormalizeOptionalText(
            customerName,
            CustomerNameMaxLength,
            nameof(customerName));
        CustomerIdentificationNumber = NormalizeOptionalText(
            customerIdentificationNumber,
            CustomerIdentificationNumberMaxLength,
            nameof(customerIdentificationNumber));
        Comment = NormalizeOptionalText(
            comment,
            CommentMaxLength,
            nameof(comment));
    }

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

    public SaleItem UpdateItem(
        long saleItemId,
        string productName,
        decimal quantity,
        decimal unitPrice,
        string? comment = null)
    {
        EnsureDraft();

        var item = GetItem(saleItemId);
        item.UpdateDetails(productName, quantity, unitPrice, comment);
        RecalculateTotal();

        return item;
    }

    public SaleItem RemoveItem(long saleItemId)
    {
        EnsureDraft();

        var item = GetItem(saleItemId);
        _items.Remove(item);
        RecalculateTotal();

        return item;
    }

    public void Complete(
        IEnumerable<SalePaymentAllocation> payments,
        DateTime dateCompleted)
    {
        EnsureDraft();

        ArgumentNullException.ThrowIfNull(payments);

        if (_items.Count == 0)
        {
            throw new InvalidOperationException(
                "A sale must contain at least one item before it can be completed.");
        }

        if (_payments.Count != 0)
        {
            throw new InvalidOperationException(
                "A draft sale cannot contain existing payments.");
        }

        var newPayments = payments
            .Select(payment => SalePayment.Create(
                Id,
                payment.PaymentType,
                payment.Amount))
            .ToArray();

        if (newPayments.Length == 0)
        {
            throw new InvalidOperationException(
                "At least one payment is required to complete a sale.");
        }

        var paymentTotal = SalePayment.RoundAmount(
            newPayments.Sum(payment => payment.Amount));
        var saleTotal = SalePayment.RoundAmount(TotalAmount);

        if (paymentTotal != saleTotal)
        {
            throw new InvalidOperationException(
                "The payment total must equal the sale total.");
        }

        _payments.AddRange(newPayments);
        Status = SaleStatus.Completed;
        DateCompleted = dateCompleted;
        DateCancelled = null;
    }

    public void Cancel(DateTime dateCancelled)
    {
        EnsureDraft();

        if (_payments.Count != 0)
        {
            throw new InvalidOperationException(
                "A draft sale with payments cannot be cancelled.");
        }

        Status = SaleStatus.Cancelled;
        DateCompleted = null;
        DateCancelled = dateCancelled;
    }

    public void Reopen()
    {
        if (Status != SaleStatus.Completed)
        {
            throw new InvalidOperationException(
                "Only a completed sale can be reopened.");
        }

        _payments.Clear();
        Status = SaleStatus.Draft;
        DateCompleted = null;
        DateCancelled = null;
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

    private SaleItem GetItem(long saleItemId)
        => _items.SingleOrDefault(item => item.Id == saleItemId)
           ?? throw new KeyNotFoundException(
               $"Sale item with ID {saleItemId} does not belong to this sale.");

    private static string? NormalizeOptionalText(
        string? value,
        int maxLength,
        string parameterName)
    {
        var normalizedValue = string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();

        if (normalizedValue?.Length > maxLength)
        {
            throw new ArgumentException(
                $"Value cannot exceed {maxLength} characters.",
                parameterName);
        }

        return normalizedValue;
    }
}
