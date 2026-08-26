using FluentValidation;

namespace StorePos.Application.Products.Queries.GetList;

public sealed class GetProductsQueryValidator : AbstractValidator<GetProductsQuery>
{
    public GetProductsQueryValidator()
    {
        RuleFor(query => query.Status).IsInEnum();
        RuleFor(query => query.MeasurementUnitId)
            .GreaterThan(0)
            .When(query => query.MeasurementUnitId.HasValue);
        RuleFor(query => query.PriceFrom)
            .GreaterThanOrEqualTo(0)
            .When(query => query.PriceFrom.HasValue);
        RuleFor(query => query.PriceTo)
            .GreaterThanOrEqualTo(0)
            .When(query => query.PriceTo.HasValue);
        RuleFor(query => query.PriceTo)
            .GreaterThanOrEqualTo(query => query.PriceFrom!.Value)
            .When(query => query.PriceFrom.HasValue && query.PriceTo.HasValue)
            .WithMessage("საწყისი ფასი საბოლოო ფასს არ უნდა აღემატებოდეს.");
        RuleFor(query => query.PageNumber).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 200);
    }
}
