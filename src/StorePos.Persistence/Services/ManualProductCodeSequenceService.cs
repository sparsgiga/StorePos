using System.Globalization;
using Microsoft.EntityFrameworkCore;
using StorePos.Application.Common.Interfaces;
using StorePos.Persistence.Context;
using StorePos.Persistence.Sequences;

namespace StorePos.Persistence.Services;

public sealed class ManualProductCodeSequenceService(StorePosDbContext context)
    : IManualProductCodeSequenceService
{
    public async Task<string> GetSuggestedCodeAsync(
        CancellationToken cancellationToken = default)
    {
        var nextCode = await context.ManualProductCodeSequences
            .AsNoTracking()
            .Where(sequence => sequence.Id == ManualProductCodeSequence.SingletonId)
            .Select(sequence => sequence.NextCode)
            .SingleAsync(cancellationToken);

        var candidate = await GetFirstAvailableCodeAsync(nextCode, cancellationToken);
        return candidate.ToString(CultureInfo.InvariantCulture);
    }

    public async Task AdvanceIfConsumedAsync(
        string createdProductCode,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(createdProductCode);

        var sequence = context.Database.IsRelational()
            ? await context.ManualProductCodeSequences
                .FromSqlRaw(
                    """
                    SELECT [Id], [NextCode], [RowVersion]
                    FROM [dbo].[ManualProductCodeSequence] WITH (UPDLOCK, HOLDLOCK)
                    WHERE [Id] = 1
                    """)
                .SingleAsync(cancellationToken)
            : await context.ManualProductCodeSequences
                .SingleAsync(
                    current => current.Id == ManualProductCodeSequence.SingletonId,
                    cancellationToken);

        var candidate = await GetFirstAvailableCodeAsync(
            sequence.NextCode,
            cancellationToken);
        if (!string.Equals(
                createdProductCode.Trim(),
                candidate.ToString(CultureInfo.InvariantCulture),
                StringComparison.Ordinal))
        {
            return;
        }

        if (candidate == long.MaxValue)
        {
            throw new InvalidOperationException(
                "The manual product code sequence has reached its maximum value.");
        }

        sequence.AdvanceTo(candidate + 1L);
    }

    private async Task<long> GetFirstAvailableCodeAsync(
        long startingCode,
        CancellationToken cancellationToken)
    {
        if (!context.Database.IsRelational())
        {
            return await GetFirstAvailableCodeForNonRelationalProviderAsync(
                startingCode,
                cancellationToken);
        }

        return await context.Database
            .SqlQueryRaw<long>(
                """
                SELECT CONVERT(bigint,
                    CASE
                        WHEN NOT EXISTS
                        (
                            SELECT 1
                            FROM [dbo].[Products]
                            WHERE [Code] = CONVERT(nvarchar(50), {0})
                        )
                        THEN {0}
                        ELSE
                        (
                            SELECT MIN(CONVERT(decimal(20, 0), [Occupied].[NumericCode]) + 1)
                            FROM
                            (
                                SELECT
                                    [Code],
                                    TRY_CONVERT(bigint, [Code]) AS [NumericCode]
                                FROM [dbo].[Products]
                            ) AS [Occupied]
                            WHERE [Occupied].[NumericCode] >= {0}
                              AND [Occupied].[NumericCode] < 9223372036854775807
                              AND [Occupied].[Code] = CONVERT(
                                  nvarchar(50),
                                  [Occupied].[NumericCode])
                              AND NOT EXISTS
                              (
                                  SELECT 1
                                  FROM [dbo].[Products] AS [NextProduct]
                                  WHERE [NextProduct].[Code] = CONVERT(
                                      nvarchar(50),
                                      CONVERT(decimal(20, 0), [Occupied].[NumericCode]) + 1)
                              )
                        )
                    END) AS [Value]
                """,
                startingCode)
            .SingleAsync(cancellationToken);
    }

    private async Task<long> GetFirstAvailableCodeForNonRelationalProviderAsync(
        long startingCode,
        CancellationToken cancellationToken)
    {
        var occupiedCodes = await context.Products
            .AsNoTracking()
            .Select(product => product.Code)
            .ToArrayAsync(cancellationToken);
        var occupied = occupiedCodes.ToHashSet(StringComparer.Ordinal);
        var candidate = startingCode;

        while (occupied.Contains(candidate.ToString(CultureInfo.InvariantCulture)))
        {
            if (candidate == long.MaxValue)
            {
                throw new InvalidOperationException(
                    "The manual product code sequence has reached its maximum value.");
            }

            candidate++;
        }

        return candidate;
    }
}
