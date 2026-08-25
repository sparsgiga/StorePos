using Microsoft.EntityFrameworkCore;
using StorePos.Application.Common.Interfaces;
using StorePos.Application.Common.Models;
using StorePos.Application.Sales.Queries.GetDetails;
using StorePos.Application.Sales.Queries.GetHistory;
using StorePos.Application.Sales.Queries.GetSoldProducts;
using StorePos.Domain.Enums;
using StorePos.Persistence.Context;

namespace StorePos.Persistence.Queries;

public sealed class SalesReadService(
    StorePosDbContext context,
    TimeProvider timeProvider) : ISalesReadService
{
    public async Task<PagedResult<SalesHistoryItemModel>> GetHistoryAsync(
        GetSalesHistoryQuery request,
        CancellationToken cancellationToken = default)
    {
        var query = context.Sales.AsNoTracking();
        var saleNumber = NormalizeSearch(request.SaleNumber);
        var customerName = NormalizeSearch(request.CustomerName);
        var (dateFromUtc, dateToExclusiveUtc) = GetUtcRange(
            request.DateFrom,
            request.DateTo);

        if (request.Status.HasValue)
        {
            query = query.Where(sale => sale.Status == request.Status.Value);
        }

        if (saleNumber is not null)
        {
            query = query.Where(sale => sale.SaleNumber.Contains(saleNumber));
        }

        if (customerName is not null)
        {
            query = query.Where(sale =>
                sale.CustomerName != null && sale.CustomerName.Contains(customerName));
        }

        if (dateFromUtc.HasValue)
        {
            query = query.Where(sale =>
                (sale.Status == SaleStatus.Completed
                    ? sale.DateCompleted ?? sale.DateCreated
                    : sale.Status == SaleStatus.Cancelled
                        ? sale.DateCancelled ?? sale.DateCreated
                        : sale.DateCreated) >= dateFromUtc.Value);
        }

        if (dateToExclusiveUtc.HasValue)
        {
            query = query.Where(sale =>
                (sale.Status == SaleStatus.Completed
                    ? sale.DateCompleted ?? sale.DateCreated
                    : sale.Status == SaleStatus.Cancelled
                        ? sale.DateCancelled ?? sale.DateCreated
                        : sale.DateCreated) < dateToExclusiveUtc.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(sale =>
                sale.Status == SaleStatus.Completed
                    ? sale.DateCompleted ?? sale.DateCreated
                    : sale.Status == SaleStatus.Cancelled
                        ? sale.DateCancelled ?? sale.DateCreated
                        : sale.DateCreated)
            .ThenByDescending(sale => sale.Id)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(sale => new SalesHistoryItemModel(
                sale.Id,
                sale.SaleNumber,
                sale.Status,
                sale.CustomerName,
                sale.CustomerIdentificationNumber,
                sale.TotalAmount,
                sale.DateCreated,
                sale.DateCompleted,
                sale.DateCancelled,
                sale.Status == SaleStatus.Completed
                    ? sale.DateCompleted ?? sale.DateCreated
                    : sale.Status == SaleStatus.Cancelled
                        ? sale.DateCancelled ?? sale.DateCreated
                        : sale.DateCreated,
                sale.Payments
                    .Where(payment => payment.PaymentType == PaymentType.Cash)
                    .Sum(payment => (decimal?)payment.Amount) ?? 0m,
                sale.Payments
                    .Where(payment => payment.PaymentType == PaymentType.Card)
                    .Sum(payment => (decimal?)payment.Amount) ?? 0m,
                sale.Payments
                    .Where(payment => payment.PaymentType == PaymentType.BankTransfer)
                    .Sum(payment => (decimal?)payment.Amount) ?? 0m,
                sale.Payments
                    .Where(payment => payment.PaymentType == PaymentType.Other)
                    .Sum(payment => (decimal?)payment.Amount) ?? 0m))
            .ToArrayAsync(cancellationToken);

        return new PagedResult<SalesHistoryItemModel>(
            items,
            totalCount,
            request.PageNumber,
            request.PageSize);
    }

    public async Task<PagedResult<SoldProductModel>> GetSoldProductsAsync(
        GetSoldProductsQuery request,
        CancellationToken cancellationToken = default)
    {
        var productSearch = NormalizeSearch(request.ProductSearch);
        var saleNumber = NormalizeSearch(request.SaleNumber);
        var customerName = NormalizeSearch(request.CustomerName);
        var (dateFromUtc, dateToExclusiveUtc) = GetUtcRange(
            request.DateFrom,
            request.DateTo);

        var query =
            from item in context.SaleItems.AsNoTracking()
            join sale in context.Sales.AsNoTracking()
                on item.SaleId equals sale.Id
            where sale.Status == SaleStatus.Completed && sale.DateCompleted.HasValue
            select new { Sale = sale, Item = item };

        if (dateFromUtc.HasValue)
        {
            query = query.Where(row => row.Sale.DateCompleted >= dateFromUtc.Value);
        }

        if (dateToExclusiveUtc.HasValue)
        {
            query = query.Where(row => row.Sale.DateCompleted < dateToExclusiveUtc.Value);
        }

        if (productSearch is not null)
        {
            query = query.Where(row =>
                row.Item.ProductName.Contains(productSearch) ||
                row.Item.ProductCode != null && row.Item.ProductCode.Contains(productSearch) ||
                row.Item.Barcode != null && row.Item.Barcode.Contains(productSearch));
        }

        if (saleNumber is not null)
        {
            query = query.Where(row => row.Sale.SaleNumber.Contains(saleNumber));
        }

        if (customerName is not null)
        {
            query = query.Where(row =>
                row.Sale.CustomerName != null &&
                row.Sale.CustomerName.Contains(customerName));
        }

        if (request.IsManual.HasValue)
        {
            query = query.Where(row => row.Item.IsManual == request.IsManual.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(row => row.Sale.DateCompleted)
            .ThenByDescending(row => row.Sale.Id)
            .ThenByDescending(row => row.Item.Id)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(row => new SoldProductModel(
                row.Sale.Id,
                row.Item.Id,
                row.Sale.SaleNumber,
                row.Sale.DateCompleted!.Value,
                row.Sale.CustomerName,
                row.Item.ProductId,
                row.Item.ProductCode,
                row.Item.Barcode,
                row.Item.ProductName,
                row.Item.MeasurementUnitName,
                row.Item.Quantity,
                row.Item.UnitPrice,
                row.Item.LineTotal,
                row.Item.IsManual,
                row.Item.Comment))
            .ToArrayAsync(cancellationToken);

        return new PagedResult<SoldProductModel>(
            items,
            totalCount,
            request.PageNumber,
            request.PageSize);
    }

    public async Task<SaleDetailsModel?> GetDetailsAsync(
        long saleId,
        CancellationToken cancellationToken = default)
    {
        var sale = await context.Sales
            .AsNoTracking()
            .Where(currentSale => currentSale.Id == saleId)
            .Select(currentSale => new
            {
                currentSale.Id,
                currentSale.SaleNumber,
                currentSale.Status,
                currentSale.CustomerName,
                currentSale.CustomerIdentificationNumber,
                currentSale.Comment,
                currentSale.TotalAmount,
                currentSale.DateCreated,
                currentSale.DateCompleted,
                currentSale.DateCancelled
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (sale is null)
        {
            return null;
        }

        var items = await context.SaleItems
            .AsNoTracking()
            .Where(item => item.SaleId == saleId)
            .OrderBy(item => item.Id)
            .Select(item => new SaleDetailsItemModel(
                item.Id,
                item.ProductId,
                item.ProductCode,
                item.Barcode,
                item.ProductName,
                item.MeasurementUnitId,
                item.MeasurementUnitName,
                item.Quantity,
                item.UnitPrice,
                item.LineTotal,
                item.IsManual,
                item.Comment))
            .ToArrayAsync(cancellationToken);

        var payments = await context.SalePayments
            .AsNoTracking()
            .Where(payment => payment.SaleId == saleId)
            .OrderBy(payment => payment.Id)
            .Select(payment => new SaleDetailsPaymentModel(
                payment.PaymentType,
                payment.Amount))
            .ToArrayAsync(cancellationToken);

        return new SaleDetailsModel(
            sale.Id,
            sale.SaleNumber,
            sale.Status,
            sale.CustomerName,
            sale.CustomerIdentificationNumber,
            sale.Comment,
            sale.TotalAmount,
            sale.DateCreated,
            sale.DateCompleted,
            sale.DateCancelled,
            items,
            payments);
    }

    private (DateTime? DateFromUtc, DateTime? DateToExclusiveUtc) GetUtcRange(
        DateOnly? dateFrom,
        DateOnly? dateTo)
        => (
            dateFrom.HasValue ? ToUtc(dateFrom.Value) : null,
            dateTo.HasValue ? ToUtc(dateTo.Value.AddDays(1)) : null);

    private DateTime ToUtc(DateOnly date)
    {
        var localDateTime = DateTime.SpecifyKind(
            date.ToDateTime(TimeOnly.MinValue),
            DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(localDateTime, timeProvider.LocalTimeZone);
    }

    private static string? NormalizeSearch(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
