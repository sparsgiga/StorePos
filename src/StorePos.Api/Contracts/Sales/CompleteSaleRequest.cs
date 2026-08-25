using System.ComponentModel.DataAnnotations;
using StorePos.Domain.Enums;

namespace StorePos.Api.Contracts.Sales;

public sealed record CompleteSaleRequest(
    [Required, MinLength(1)]
    IReadOnlyList<CompleteSalePaymentRequest> Payments);

public sealed record CompleteSalePaymentRequest(
    [EnumDataType(typeof(PaymentType))]
    PaymentType PaymentType,
    [Range(typeof(decimal), "0", "9999999999999.99999")]
    decimal Amount);
