using FluentValidation;

namespace StorePos.Application.Products.Queries.GetById;

public sealed class GetProductByIdQueryValidator : AbstractValidator<GetProductByIdQuery>
{
    public GetProductByIdQueryValidator()
        => RuleFor(query => query.ProductId).GreaterThan(0);
}
