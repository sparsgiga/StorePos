using MediatR;

namespace StorePos.Application.Sales.Commands.CreateDraft;

public sealed record CreateDraftSaleCommand(
    long? CashierId = null,
    string? CustomerName = null,
    string? CustomerIdentificationNumber = null,
    string? Comment = null) : IRequest<CreateDraftSaleResult>;
