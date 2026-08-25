using FluentValidation;
using StorePos.Domain.Aggregates.Sale;

namespace StorePos.Application.Sales.Commands.UpdateComment;

public sealed class UpdateSaleCommentCommandValidator : AbstractValidator<UpdateSaleCommentCommand>
{
    public UpdateSaleCommentCommandValidator()
    {
        RuleFor(command => command.SaleId).GreaterThan(0);
        RuleFor(command => command.Comment).MaximumLength(Sale.CommentMaxLength);
    }
}
