namespace StorePos.Desktop.History.Models;

public sealed record SaleDetailsDto(
    long Id,
    string SaleNumber,
    int Status,
    long? CustomerId,
    string? CustomerName,
    string? CustomerIdentificationNumber,
    string? Comment,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal OutstandingAmount,
    bool HasDebt,
    DateTime DateCreated,
    DateTime? DateCompleted,
    DateTime? DateCancelled,
    IReadOnlyList<SaleDetailsItemDto> Items,
    IReadOnlyList<SaleDetailsPaymentDto> Payments)
{
    public string StatusName => Status switch
    {
        1 => "Draft",
        2 => "დასრულებული",
        3 => "გაუქმებული",
        _ => "უცნობი"
    };
}

public sealed record SaleDetailsItemDto(
    long Id,
    long? ProductId,
    string? ProductCode,
    string? Barcode,
    string ProductName,
    int? UnitId,
    string? UnitName,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    bool IsManual,
    string? Comment)
{
    public string SourceName => IsManual ? "ხელით" : "კატალოგი";
}

public sealed record SaleDetailsPaymentDto(
    int PaymentType,
    int PaymentKind,
    decimal Amount,
    DateTime DateCreated)
{
    public string PaymentTypeName => PaymentType switch
    {
        1 => "ნაღდი",
        2 => "ბარათი",
        3 => "გადარიცხვა",
        4 => "სხვა",
        _ => "უცნობი"
    };

    public string PaymentKindName => PaymentKind switch
    {
        1 => "დასრულებისას",
        2 => "ვალის დაფარვა",
        _ => "უცნობი"
    };
}

public sealed record ReopenSaleResponse(
    long SaleId,
    string SaleNumber,
    int Status,
    decimal TotalAmount);
