using StorePos.Desktop.History.Models;
using StorePos.Desktop.Reporting.Models;
using StorePos.Desktop.Sales.Calculations;
using StorePos.Desktop.Sales.ViewModels;

namespace StorePos.Desktop.Reporting;

public static class SaleReportModelFactory
{
    public static FullSaleReportModel FromCurrentSale(
        SaleTabViewModel sale,
        DateTime printedAt)
    {
        ArgumentNullException.ThrowIfNull(sale);

        var paymentState = sale.PreviousCompletionPaymentState;
        return new FullSaleReportModel(
            sale.Id,
            sale.SaleNumber,
            Status: 1,
            sale.CustomerName,
            sale.CustomerIdentificationNumber,
            sale.Comment,
            sale.DateCreated,
            DateCompleted: null,
            DateCancelled: null,
            printedAt,
            sale.TotalAmount,
            sale.PaidAmount,
            sale.OutstandingAmount,
            paymentState?.CashAmount ?? 0m,
            paymentState?.CardAmount ?? 0m,
            paymentState?.BankTransferAmount ?? 0m,
            paymentState?.OtherAmount ?? 0m,
            sale.Items.Select(item => new FullSaleReportItemModel(
                item.Id,
                item.ProductCode,
                item.Barcode,
                item.ProductName,
                item.MeasurementUnitName,
                item.Quantity,
                item.UnitPrice,
                item.LineTotal,
                item.IsManual,
                item.Comment)).ToArray());
    }

    public static FullSaleReportModel FromSaleDetails(
        SaleDetailsDto sale,
        DateTime printedAt)
    {
        ArgumentNullException.ThrowIfNull(sale);

        var currentPayments = sale.CompletionVersion > 0
            ? sale.Payments.Where(payment =>
                payment.CompletionVersion == sale.CompletionVersion)
            : [];

        return new FullSaleReportModel(
            sale.Id,
            sale.SaleNumber,
            sale.Status,
            sale.CustomerName,
            sale.CustomerIdentificationNumber,
            sale.Comment,
            sale.DateCreated,
            sale.DateCompleted,
            sale.DateCancelled,
            printedAt,
            sale.TotalAmount,
            sale.PaidAmount,
            sale.OutstandingAmount,
            SumByType(currentPayments, 1),
            SumByType(currentPayments, 2),
            SumByType(currentPayments, 3),
            SumByType(currentPayments, 4),
            sale.Items.Select(item => new FullSaleReportItemModel(
                item.Id,
                item.ProductCode,
                item.Barcode,
                item.ProductName,
                item.UnitName,
                item.Quantity,
                item.UnitPrice,
                item.LineTotal,
                item.IsManual,
                item.Comment)).ToArray());
    }

    private static decimal SumByType(
        IEnumerable<SaleDetailsPaymentDto> payments,
        int paymentType)
        => FinancialInputPrecision.RoundMoney(
            payments
                .Where(payment => payment.PaymentType == paymentType)
                .Sum(payment => payment.Amount));
}
