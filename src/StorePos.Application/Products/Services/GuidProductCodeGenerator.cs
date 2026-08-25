using StorePos.Application.Common.Interfaces;

namespace StorePos.Application.Products.Services;

public sealed class GuidProductCodeGenerator : IProductCodeGenerator
{
    public const string Prefix = "PRD-";

    public string Generate()
        => $"{Prefix}{Guid.NewGuid():N}".ToUpperInvariant();
}
