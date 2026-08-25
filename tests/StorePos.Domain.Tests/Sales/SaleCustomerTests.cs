using StorePos.Domain.Aggregates.Sale;
using StorePos.Domain.Enums;

namespace StorePos.Domain.Tests.Sales;

public sealed class SaleCustomerTests
{
    [Fact]
    public void AssignCustomer_DraftSale_StoresReferenceAndSnapshotsWithoutChangingComment()
    {
        var sale = Sale.Create("20260825-0001", comment: "Sale comment");
        sale.AssignCustomer(15, "  Old Name  ", "  01001  ");

        Assert.Equal(15, sale.CustomerId);
        Assert.Equal("Old Name", sale.CustomerName);
        Assert.Equal("01001", sale.CustomerIdentificationNumber);
        Assert.Equal("Sale comment", sale.Comment);
    }

    [Fact]
    public void RemoveCustomer_DraftSale_ClearsCustomerWithoutChangingComment()
    {
        var sale = Sale.Create("20260825-0001", comment: "Sale comment");
        sale.AssignCustomer(15, "Customer", "01001");
        sale.RemoveCustomer();

        Assert.Null(sale.CustomerId);
        Assert.Null(sale.CustomerName);
        Assert.Null(sale.CustomerIdentificationNumber);
        Assert.Equal("Sale comment", sale.Comment);
    }

    [Theory]
    [InlineData(SaleStatus.Completed)]
    [InlineData(SaleStatus.Cancelled)]
    public void CustomerCannotBeChangedWhenSaleIsNotDraft(SaleStatus status)
    {
        var sale = Sale.Create("20260825-0001");
        if (status == SaleStatus.Completed)
        {
            sale.AddManualItem("Product", 1m, 1m);
            sale.Complete(
                [new SalePaymentAllocation(PaymentType.Cash, 1m)],
                DateTime.UtcNow);
        }
        else
        {
            sale.Cancel(DateTime.UtcNow);
        }

        Assert.Throws<InvalidOperationException>(() =>
            sale.AssignCustomer(15, "Customer", null));
        Assert.Throws<InvalidOperationException>(sale.RemoveCustomer);
    }
}
