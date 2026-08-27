using System.Globalization;
using Microsoft.EntityFrameworkCore;
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
                    SELECT COALESCE(
                        CONVERT(
                            nvarchar(50),
                            CASE
                                WHEN MAX([NumericCode]) < 9223372036854775807
                                THEN CONVERT(decimal(20, 0), MAX([NumericCode])) + 1
                            END),
                        N'') AS [Value]
                    FROM
                    (
                        SELECT TRY_CONVERT(bigint, [Code]) AS [NumericCode]
                        FROM [dbo].[Products]
                    ) AS [NumericProductCodes]
                    WHERE [NumericCode] > 0
                    """)
                .SingleAsync(cancellationToken);
        }

        // EF Core's non-relational test provider cannot execute the SQL Server query.
        var codes = await context.Products
            .AsNoTracking()
            .Select(product => product.Code)
            .ToArrayAsync(cancellationToken);

        long? maximumCode = null;
        foreach (var code in codes)
        {
            if (long.TryParse(
                    code,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var numericCode) &&
                numericCode > 0 &&
                (!maximumCode.HasValue || numericCode > maximumCode.Value))
            {
                maximumCode = numericCode;
            }
        }

        return maximumCode is > 0 and < long.MaxValue
            ? (maximumCode.Value + 1L).ToString(CultureInfo.InvariantCulture)
            : string.Empty;
    }
}
