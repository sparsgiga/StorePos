using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StorePos.Application.Common.Interfaces;
using StorePos.Domain.Aggregates.MeasurementUnit;
using StorePos.Domain.Aggregates.Product;
using StorePos.Domain.Aggregates.Sale;
using StorePos.Domain.Aggregates.User;
using StorePos.Domain.Interfaces;
using StorePos.Persistence.Context;
using StorePos.Persistence.Repositories;
using StorePos.Persistence.Queries;
using StorePos.Persistence.Services;

namespace StorePos.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<StorePosDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IMeasurementUnitRepository, MeasurementUnitRepository>();
        services.AddScoped<ISaleRepository, SaleRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISaleNumberGenerator, SaleNumberGenerator>();
        services.AddScoped<ISalesReadService, SalesReadService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.TryAddSingleton(TimeProvider.System);

        return services;
    }
}
