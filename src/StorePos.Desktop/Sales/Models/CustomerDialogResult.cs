namespace StorePos.Desktop.Sales.Models;

public sealed record CustomerDialogResult(
    long? CustomerId,
    string? CustomerName,
    string? CustomerIdentificationNumber,
    string? SaleComment);
