using System.ComponentModel.DataAnnotations;
using StorePos.Domain.Aggregates.Sale;

namespace StorePos.Api.Contracts.Sales;

public sealed class UpdateSaleItemRequest
{
    [Required]
    [StringLength(SaleItem.ProductNameMaxLength)]
    public string ProductName { get; init; } = string.Empty;

    [Range(typeof(decimal), "0.00001", "9999999999999.99999")]
    public decimal Quantity { get; init; }

    [Range(typeof(decimal), "0.00001", "9999999999999.99999")]
    public decimal UnitPrice { get; init; }

    [StringLength(SaleItem.CommentMaxLength)]
    public string? Comment { get; init; }
}
