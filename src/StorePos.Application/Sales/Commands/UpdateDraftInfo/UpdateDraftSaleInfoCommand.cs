using MediatR;

namespace StorePos.Application.Sales.Commands.UpdateDraftInfo;

public sealed record UpdateDraftSaleInfoCommand(
    long SaleId,
    string? CustomerName,
    string? CustomerIdentificationNumber,
    string? Comment) : IRequest<UpdateDraftSaleInfoResult?>;
