using StorePos.Domain.Aggregates.Customer;

namespace StorePos.Domain.Tests.Customers;

public sealed class CustomerTests
{
    [Fact]
    public void Create_RequiresName()
        => Assert.Throws<ArgumentException>(() => Customer.Create(" "));

    [Fact]
    public void Create_AllowsNullOptionalValues()
    {
        var customer = Customer.Create("Customer");
        Assert.Null(customer.IdentificationNumber);
        Assert.Null(customer.Information);
    }

    [Fact]
    public void Create_TrimsAllValues()
    {
        var customer = Customer.Create(
            "  Customer  ",
            "  01001  ",
            "  Needs invoice  ");

        Assert.Equal("Customer", customer.Name);
        Assert.Equal("01001", customer.IdentificationNumber);
        Assert.Equal("Needs invoice", customer.Information);
    }

    [Fact]
    public void Update_ChangesNameIdentificationNumberAndInformation()
    {
        var customer = Customer.Create("Old Name");
        customer.Update("New Name", "123", "Updated information");

        Assert.Equal("New Name", customer.Name);
        Assert.Equal("123", customer.IdentificationNumber);
        Assert.Equal("Updated information", customer.Information);
    }

    [Fact]
    public void Update_EmptyOptionalValues_NormalizesToNull()
    {
        var customer = Customer.Create("Customer", "123", "Information");
        customer.Update("Customer", " ", "");

        Assert.Null(customer.IdentificationNumber);
        Assert.Null(customer.Information);
    }
}
