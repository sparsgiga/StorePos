using System.ComponentModel.DataAnnotations;
using StorePos.Domain.Enums;

namespace StorePos.Api.Contracts.Sales;

public sealed record AddDebtPaymentRequest(
    [EnumDataType(typeof(PaymentType))]
    PaymentType PaymentType,
    [Range(typeof(decimal), "0.00001", "9999999999999.99999")]
    decimal Amount);
