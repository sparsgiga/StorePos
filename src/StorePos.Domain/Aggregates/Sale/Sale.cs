using StorePos.Domain.Base;
using StorePos.Domain.Common;
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
        CustomerName = NormalizeOptionalText(
            customerName,
            CustomerNameMaxLength,
            nameof(customerName));
        CustomerIdentificationNumber = NormalizeOptionalText(
            customerIdentificationNumber,
            CustomerIdentificationNumberMaxLength,
            nameof(customerIdentificationNumber));
        Comment = NormalizeOptionalText(comment, CommentMaxLength, nameof(comment));
    }

    public string SaleNumber { get; private set; } = string.Empty;

    public SaleStatus Status { get; private set; }

    public long? CashierId { get; private set; }

    public long? CustomerId { get; private set; }

    public string? CustomerName { get; private set; }

    public string? CustomerIdentificationNumber { get; private set; }

    public decimal TotalAmount { get; private set; }

    public string? Comment { get; private set; }

    public DateTime? DateCompleted { get; private set; }

    public DateTime? DateCancelled { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public long FinancialRevision { get; private set; }

    public int CompletionVersion { get; private set; }

    public IReadOnlyCollection<SaleItem> Items => _items;

    public IReadOnlyCollection<SalePayment> Payments => _payments;

    public decimal PaidAmount { get; private set; }

    public decimal OutstandingAmount { get; private set; }

    public bool HasDebt => OutstandingAmount > 0;

    public static Sale Create(
        string saleNumber,
        long? cashierId = null,
        string? customerName = null,
        string? customerIdentificationNumber = null,
        string? comment = null)
        => new(saleNumber, cashierId, customerName, customerIdentificationNumber, comment);

    public void AssignCustomer(
        long customerId,
        string customerName,
        string? customerIdentificationNumber)
    {
        EnsureDraft();

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(customerId);

        if (string.IsNullOrWhiteSpace(customerName))
        {
            throw new ArgumentException("Customer name is required.", nameof(customerName));
        }

        var normalizedName = customerName.Trim();
        if (normalizedName.Length > CustomerNameMaxLength)
        {
            throw new ArgumentException(
                $"Value cannot exceed {CustomerNameMaxLength} characters.",
                nameof(customerName));
        }

        CustomerId = customerId;
        CustomerName = normalizedName;
        CustomerIdentificationNumber = NormalizeOptionalText(
            customerIdentificationNumber,
            CustomerIdentificationNumberMaxLength,
            nameof(customerIdentificationNumber));
    }

    public void RemoveCustomer()
    {
        EnsureDraft();

        CustomerId = null;
        CustomerName = null;
        CustomerIdentificationNumber = null;
    }

    public void UpdateComment(string? comment)
    {
        EnsureDraft();
        Comment = NormalizeOptionalText(comment, CommentMaxLength, nameof(comment));
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

    public CatalogSaleItemAddition AddProductItem(
        long productId,
        string productCode,
        string? barcode,
        string productName,
        int measurementUnitId,
        string measurementUnitName,
        decimal quantity,
        decimal unitPrice,
        string? comment = null)
    {
        EnsureDraft();
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(productId);

        var existingItem = _items.SingleOrDefault(item => item.ProductId == productId);
        if (existingItem is not null)
        {
            existingItem.IncreaseQuantity(quantity);
            RecalculateTotal();
            return new CatalogSaleItemAddition(existingItem, false);
        }

        var item = SaleItem.CreateCatalog(
            Id,
            productId,
            productCode,
            barcode,
            productName,
            measurementUnitId,
            measurementUnitName,
            quantity,
            unitPrice,
            comment);

        _items.Add(item);
        RecalculateTotal();

        return new CatalogSaleItemAddition(item, true);
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
        DateTime dateCompleted,
        bool allowDebt = false)
    {
        EnsureDraft();

        ArgumentNullException.ThrowIfNull(payments);

        if (_items.Count == 0)
        {
            throw new InvalidOperationException(
                "A sale must contain at least one item before it can be completed.");
        }

        EnsurePaymentVersionsAreValid();

        var nextCompletionVersion = checked(CompletionVersion + 1);

        var allocations = payments.ToArray();
        foreach (var payment in allocations)
        {
            if (!Enum.IsDefined(payment.PaymentType))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(payments),
                    "Payment type is not supported.");
            }

            if (payment.Amount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(payments),
                    "Payment amount cannot be negative.");
            }
        }

        var newPayments = allocations
            .Where(payment => SalePayment.RoundAmount(payment.Amount) > 0)
            .Select(payment => SalePayment.CreateCompletion(
                Id,
                nextCompletionVersion,
                payment.PaymentType,
                payment.Amount))
            .ToArray();

        var paymentTotal = FinancialPrecision.SumMoney(
            newPayments.Select(payment => payment.Amount));
        var saleTotal = FinancialPrecision.RoundMoney(TotalAmount);

        if (paymentTotal > saleTotal)
        {
            throw new InvalidOperationException(
                "The payment total cannot exceed the sale total.");
        }

        var outstandingAmount = FinancialPrecision.RoundMoney(saleTotal - paymentTotal);

        if (!allowDebt && outstandingAmount != 0)
        {
            throw new InvalidOperationException(
                "The payment total must equal the sale total.");
        }

        if (outstandingAmount > 0 && CustomerId is null)
        {
            throw new InvalidOperationException(
                "A customer must be assigned when completing a sale with debt.");
        }

        _payments.AddRange(newPayments);
        CompletionVersion = nextCompletionVersion;
        PaidAmount = paymentTotal;
        OutstandingAmount = outstandingAmount;
        Status = SaleStatus.Completed;
        DateCompleted = dateCompleted;
        DateCancelled = null;
    }

    public SalePayment AddDebtPayment(
        Guid operationId,
        PaymentType paymentType,
        decimal amount)
    {
        if (Status != SaleStatus.Completed)
        {
            throw new InvalidOperationException(
                "Debt payments can only be added to completed sales.");
        }

        EnsurePaymentVersionsAreValid();

        if (operationId == Guid.Empty)
        {
            throw new ArgumentException("Operation ID is required.", nameof(operationId));
        }

        var normalizedAmount = FinancialPrecision.RoundMoney(amount);
        var existingPayment = _payments.SingleOrDefault(payment =>
            payment.OperationId == operationId);
        if (existingPayment is not null)
        {
            if (existingPayment.PaymentType != paymentType ||
                existingPayment.Amount != normalizedAmount)
            {
                throw new InvalidOperationException(
                    "The debt payment operation was already used with different details.");
            }

            return existingPayment;
        }

        var outstandingAmount = OutstandingAmount;
        if (outstandingAmount <= 0)
        {
            throw new InvalidOperationException("The sale has no outstanding debt.");
        }

        var payment = SalePayment.CreateDebtRepayment(
            Id,
            CompletionVersion,
            paymentType,
            amount,
            operationId);

        if (payment.Amount > outstandingAmount)
        {
            throw new InvalidOperationException(
                "The debt payment cannot exceed the outstanding amount.");
        }

        _payments.Add(payment);
        PaidAmount = FinancialPrecision.SumMoney([PaidAmount, payment.Amount]);
        RefreshOutstandingAmount();
        FinancialRevision = checked(FinancialRevision + 1);
        return payment;
    }

    public void Cancel(DateTime dateCancelled)
    {
        EnsureDraft();
        EnsurePaymentVersionsAreValid();

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

        EnsurePaymentVersionsAreValid();

        Status = SaleStatus.Draft;
        DateCompleted = null;
        DateCancelled = null;
        RefreshOutstandingAmount();
    }

    private void EnsureDraft()
    {
        if (Status != SaleStatus.Draft)
        {
            throw new InvalidOperationException("Only a draft sale can be modified.");
        }
    }

    private void RecalculateTotal()
    {
        TotalAmount = FinancialPrecision.SumMoney(
            _items.Select(item => item.LineTotal));

        if (CompletionVersion == 0)
        {
            PaidAmount = 0m;
            OutstandingAmount = 0m;
            return;
        }

        RefreshOutstandingAmount();
    }

    private void RefreshOutstandingAmount()
        => OutstandingAmount = Math.Max(
            FinancialPrecision.RoundMoney(TotalAmount - PaidAmount),
            0m);

    private void EnsurePaymentVersionsAreValid()
    {
        if (CompletionVersion < 0)
        {
            throw new InvalidOperationException(
                "A sale completion version cannot be negative.");
        }

        if (Status == SaleStatus.Completed && CompletionVersion == 0)
        {
            throw new InvalidOperationException(
                "A completed sale must have a completion version.");
        }

        if (_payments.Any(payment =>
                payment.CompletionVersion <= 0 ||
                payment.CompletionVersion > CompletionVersion))
        {
            throw new InvalidOperationException(
                "The sale contains an invalid payment completion version.");
        }
    }

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
