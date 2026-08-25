using System.ComponentModel.DataAnnotations;
using StorePos.Domain.Aggregates.Sale;

namespace StorePos.Api.Contracts.Sales;

public sealed class UpdateDraftSaleInfoRequest
{
    [StringLength(Sale.CustomerNameMaxLength)]
    public string? CustomerName { get; init; }

    [StringLength(Sale.CustomerIdentificationNumberMaxLength)]
    public string? CustomerIdentificationNumber { get; init; }

    [StringLength(Sale.CommentMaxLength)]
    public string? Comment { get; init; }
}
