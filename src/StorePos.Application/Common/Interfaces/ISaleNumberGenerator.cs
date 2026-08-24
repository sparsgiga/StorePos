namespace StorePos.Application.Common.Interfaces;

public interface ISaleNumberGenerator
{
    Task<string> GenerateAsync(CancellationToken cancellationToken = default);
}
