namespace StorePos.Application.Common.Interfaces;

public interface IManualProductCodeSequenceService
{
    Task<string> GetSuggestedCodeAsync(
        CancellationToken cancellationToken = default);

    Task AdvanceIfConsumedAsync(
        string createdProductCode,
        CancellationToken cancellationToken = default);
}
