using MediatR;

namespace StorePos.Application.Sales.Commands.Cancel;

public sealed record CancelSaleCommand(long SaleId) : IRequest<CancelSaleResult?>;
