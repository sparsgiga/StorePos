using MediatR;

namespace StorePos.Application.Sales.Commands.UpdateComment;

public sealed record UpdateSaleCommentCommand(
    long SaleId,
    string? Comment) : IRequest<UpdateSaleCommentResult?>;
