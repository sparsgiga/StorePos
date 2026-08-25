using MediatR;

namespace StorePos.Application.Sales.Commands.Reopen;

public sealed record ReopenSaleCommand(long SaleId) : IRequest<ReopenSaleResult?>;
