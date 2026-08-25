using System.ComponentModel.DataAnnotations;

namespace StorePos.Api.Contracts.Sales;

public sealed record AssignCustomerToSaleRequest(
    [Range(1, long.MaxValue)] long CustomerId);
