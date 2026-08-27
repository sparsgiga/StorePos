using Microsoft.EntityFrameworkCore;
using StorePos.Application.Common.Interfaces;
using StorePos.Application.Products.Queries.GetCreationDefaults;
using StorePos.Persistence.Context;

namespace StorePos.Persistence.Queries;

public sealed class ProductCreationDefaultsReadService(
    StorePosDbContext context,
    IManualProductCodeSequenceService codeSequenceService)
    : IProductCreationDefaultsReadService
{
    private const string DefaultUnitName = "ცალი";
    private const string DefaultUnitShortName = "ც";

    public async Task<ProductCreationDefaultsResult> GetAsync(
        CancellationToken cancellationToken = default)
    {
        var suggestedCode = await codeSequenceService.GetSuggestedCodeAsync(cancellationToken);

        var matchingUnits = await context.MeasurementUnits
            .AsNoTracking()
            .Where(unit =>
                unit.IsActive &&
                unit.Name == DefaultUnitName &&
                unit.ShortName == DefaultUnitShortName)
            .OrderBy(unit => unit.Id)
            .Take(2)
            .Select(unit => new
            {
                unit.Id,
                unit.Name,
                unit.ShortName
            })
            .ToArrayAsync(cancellationToken);

        if (matchingUnits.Length == 1)
        {
            var unit = matchingUnits[0];
            return new ProductCreationDefaultsResult(
                suggestedCode,
                unit.Id,
                unit.Name,
                unit.ShortName,
                ConfigurationMessage: null);
        }

        var message = matchingUnits.Length == 0
            ? "აქტიური საზომი ერთეული „ცალი (ც)“ ვერ მოიძებნა. აირჩიეთ სხვა ერთეული."
            : "აქტიური საზომი ერთეული „ცალი (ც)“ ერთზე მეტია. აირჩიეთ ერთეული ხელით.";

        return new ProductCreationDefaultsResult(
            suggestedCode,
            DefaultMeasurementUnitId: null,
            DefaultMeasurementUnitName: null,
            DefaultMeasurementUnitShortName: null,
            message);
    }

}
