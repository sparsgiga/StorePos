using Microsoft.EntityFrameworkCore;
using System.Numerics;
using StorePos.Application.Common.Interfaces;
using StorePos.Application.Products.Queries.GetCreationDefaults;
using StorePos.Persistence.Context;

namespace StorePos.Persistence.Queries;

public sealed class ProductCreationDefaultsReadService(StorePosDbContext context)
    : IProductCreationDefaultsReadService
{
    private const string DefaultUnitName = "ცალი";
    private const string DefaultUnitShortName = "ც";

    public async Task<ProductCreationDefaultsResult> GetAsync(
        CancellationToken cancellationToken = default)
    {
        var suggestedCode = await GetSuggestedCodeAsync(cancellationToken);

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

    private async Task<string> GetSuggestedCodeAsync(CancellationToken cancellationToken)
    {
        if (context.Database.IsRelational())
        {
            return await context.Database
                .SqlQueryRaw<string>(
                    """
                    SELECT CONVERT(nvarchar(50),
                        COALESCE(MAX(TRY_CONVERT(decimal(38, 0), [Code])), 0) + 1) AS [Value]
                    FROM [dbo].[Products]
                    WHERE [Code] <> N''
                      AND [Code] NOT LIKE N'%[^0-9]%'
                    """)
                .SingleAsync(cancellationToken);
        }

        // EF Core's non-relational test provider cannot execute the SQL Server
        // MAX/TRY_CONVERT query. Production remains database-side.
        var codes = await context.Products
            .AsNoTracking()
            .Select(product => product.Code)
            .ToArrayAsync(cancellationToken);
        var maximum = codes
            .Where(code => code.Length > 0 && code.All(character => character is >= '0' and <= '9'))
            .Select(BigInteger.Parse)
            .DefaultIfEmpty(BigInteger.Zero)
            .Max();

        return (maximum + BigInteger.One).ToString();
    }
}
