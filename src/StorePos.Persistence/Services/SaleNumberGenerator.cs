using System.Globalization;
using Microsoft.EntityFrameworkCore;
using StorePos.Application.Common.Interfaces;
using StorePos.Persistence.Context;

namespace StorePos.Persistence.Services;

public sealed class SaleNumberGenerator(
    StorePosDbContext context,
    TimeProvider timeProvider) : ISaleNumberGenerator
{
    public async Task<string> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var businessDate = timeProvider.GetLocalNow().Date;
        var datePrefix = businessDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var saleNumberPrefix = $"{datePrefix}-";

        var existingSalesCount = await context.Sales.CountAsync(
            sale => sale.SaleNumber.StartsWith(saleNumberPrefix),
            cancellationToken);

        var sequence = existingSalesCount + 1;
        return $"{datePrefix}-{sequence:D4}";
    }
}
