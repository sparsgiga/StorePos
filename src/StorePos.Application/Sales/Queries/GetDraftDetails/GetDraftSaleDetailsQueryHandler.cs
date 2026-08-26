using MediatR;
using StorePos.Domain.Aggregates.Sale;
using StorePos.Domain.Common;
using StorePos.Domain.Enums;

namespace StorePos.Application.Sales.Queries.GetDraftDetails;

public sealed class GetDraftSaleDetailsQueryHandler(ISaleRepository saleRepository)
    : IRequestHandler<GetDraftSaleDetailsQuery, DraftSaleDetailsModel?>
{
    public async Task<DraftSaleDetailsModel?> Handle(
        GetDraftSaleDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var sale = await saleRepository.GetDraftDetailsAsync(
            request.SaleId,
            cancellationToken);

        if (sale is null)
        {
            return null;
        }

        var items = sale.Items
            .OrderBy(item => item.DateCreated)
            .ThenBy(item => item.Id)
            .Select(item => new DraftSaleItemModel(
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
            .ToArray();

        PreviousCompletionPaymentStateModel? previousPaymentState = null;
        if (sale.CompletionVersion > 0)
        {
            var previousCompletionPayments = sale.Payments
                .Where(payment =>
                    payment.CompletionVersion == sale.CompletionVersion &&
                    payment.PaymentKind == SalePaymentKind.Completion)
                .ToArray();

            previousPaymentState = new PreviousCompletionPaymentStateModel(
                sale.CompletionVersion,
                SumByType(previousCompletionPayments, PaymentType.Cash),
                SumByType(previousCompletionPayments, PaymentType.Card),
                SumByType(previousCompletionPayments, PaymentType.BankTransfer),
                SumByType(previousCompletionPayments, PaymentType.Other));
        }

        return new DraftSaleDetailsModel(
            sale.Id,
            sale.SaleNumber,
            sale.CompletionVersion,
            sale.TotalAmount,
            sale.DateCreated,
            sale.CustomerId,
            sale.CustomerName,
            sale.CustomerIdentificationNumber,
            sale.Comment,
            items,
            previousPaymentState);
    }

    private static decimal SumByType(
        IEnumerable<SalePayment> payments,
        PaymentType paymentType)
        => FinancialPrecision.SumMoney(
            payments
                .Where(payment => payment.PaymentType == paymentType)
                .Select(payment => payment.Amount));
}
