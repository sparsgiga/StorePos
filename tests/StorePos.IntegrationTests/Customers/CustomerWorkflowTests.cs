using Microsoft.EntityFrameworkCore;
using StorePos.Application.Common.Exceptions;
using StorePos.Application.Customers.Commands.Create;
using StorePos.Application.Customers.Commands.Update;
using StorePos.Application.Customers.Queries.Search;
using StorePos.Application.Sales.Commands.AssignCustomer;
using StorePos.Application.Sales.Commands.RemoveCustomer;
using StorePos.Application.Sales.Queries.GetDetails;
using StorePos.Application.Sales.Queries.GetHistory;
using StorePos.Domain.Aggregates.Customer;
using StorePos.Domain.Aggregates.Sale;
using StorePos.Domain.Enums;
using StorePos.Persistence;
using StorePos.Persistence.Context;
using StorePos.Persistence.Queries;
using StorePos.Persistence.Repositories;

namespace StorePos.IntegrationTests.Customers;

public sealed class CustomerWorkflowTests
{
    [Fact]
    public async Task CreationAndSearch_PersistAndProjectExpectedCustomers()
    {
        await using var context = CreateContext();
        var repository = new CustomerRepository(context);
        var unitOfWork = new UnitOfWork(context);
        var createHandler = new CreateCustomerCommandHandler(repository, unitOfWork);

        var exact = await createHandler.Handle(
            new CreateCustomerCommand("Giorgi Maisuradze", "01017", "Needs invoice"),
            CancellationToken.None);
        await createHandler.Handle(
            new CreateCustomerCommand("Giorgi Nozadze", null, null),
            CancellationToken.None);
        await createHandler.Handle(
            new CreateCustomerCommand("Unrelated Customer", null, null),
            CancellationToken.None);

        context.ChangeTracker.Clear();
        var searchHandler = new SearchCustomersQueryHandler(new CustomerReadService(context));
        var byName = await searchHandler.Handle(
            new SearchCustomersQuery("Giorgi"),
            CancellationToken.None);
        var byIdentificationNumber = await searchHandler.Handle(
            new SearchCustomersQuery("01017"),
            CancellationToken.None);
        var limited = await searchHandler.Handle(
            new SearchCustomersQuery("Giorgi", 1),
            CancellationToken.None);

        Assert.Equal(2, byName.Count);
        Assert.DoesNotContain(byName, customer => customer.Name == "Unrelated Customer");
        Assert.Equal(exact.Id, Assert.Single(byIdentificationNumber).Id);
        Assert.Equal("Needs invoice", Assert.Single(byIdentificationNumber).Information);
        Assert.Single(limited);
        Assert.NotEqual(default, (await context.Customers.FirstAsync()).DateCreated);
    }

    [Fact]
    public async Task DuplicateIdentificationNumber_IsRejected_AndMultipleNullsAreAllowed()
    {
        await using var context = CreateContext();
        var handler = new CreateCustomerCommandHandler(
            new CustomerRepository(context),
            new UnitOfWork(context));

        await handler.Handle(
            new CreateCustomerCommand("First", "01001"),
            CancellationToken.None);
        await handler.Handle(
            new CreateCustomerCommand("Without ID 1"),
            CancellationToken.None);
        await handler.Handle(
            new CreateCustomerCommand("Without ID 2"),
            CancellationToken.None);

        await Assert.ThrowsAsync<CustomerIdentificationNumberConflictException>(() =>
            handler.Handle(
                new CreateCustomerCommand("Duplicate", " 01001 "),
                CancellationToken.None));

        Assert.Equal(3, await context.Customers.CountAsync());
        Assert.Equal(2, await context.Customers.CountAsync(customer =>
            customer.IdentificationNumber == null));
    }

    [Fact]
    public void Model_HasFilteredUniqueIdentificationNumberAndRestrictiveSaleForeignKey()
    {
        using var context = CreateContext();
        var customerEntity = context.Model.FindEntityType(typeof(Customer))!;
        var identificationIndex = customerEntity.GetIndexes().Single(index =>
            index.Properties.Count == 1 &&
            index.Properties[0].Name == nameof(Customer.IdentificationNumber));
        var saleEntity = context.Model.FindEntityType(typeof(Sale))!;
        var customerForeignKey = saleEntity.GetForeignKeys().Single(key =>
            key.Properties.Count == 1 &&
            key.Properties[0].Name == nameof(Sale.CustomerId));

        Assert.True(identificationIndex.IsUnique);
        Assert.Equal("[IdentificationNumber] IS NOT NULL", identificationIndex.GetFilter());
        Assert.Equal(DeleteBehavior.Restrict, customerForeignKey.DeleteBehavior);
    }

    [Fact]
    public async Task AssignmentAndRemoval_PersistSnapshotsWithoutCopyingInformationOrClearingSaleComment()
    {
        await using var context = CreateContext();
        var customerRepository = new CustomerRepository(context);
        var saleRepository = new SaleRepository(context);
        var unitOfWork = new UnitOfWork(context);
        var customer = Customer.Create("Customer", "01001", "Customer information");
        var sale = Sale.Create("20260825-0001", comment: "Sale comment");
        await customerRepository.AddAsync(customer);
        await saleRepository.AddAsync(sale);
        await unitOfWork.SaveChangesAsync();

        var assignResult = await new AssignCustomerToSaleCommandHandler(
                saleRepository,
                customerRepository,
                unitOfWork)
            .Handle(
                new AssignCustomerToSaleCommand(sale.Id, customer.Id),
                CancellationToken.None);

        Assert.NotNull(assignResult);
        Assert.Equal(customer.Id, assignResult.CustomerId);
        Assert.Equal("Customer", assignResult.CustomerName);
        Assert.Equal("01001", assignResult.CustomerIdentificationNumber);
        Assert.Equal("Sale comment", assignResult.SaleComment);
        Assert.DoesNotContain("Customer information", sale.Comment ?? string.Empty);

        context.ChangeTracker.Clear();
        var removeResult = await new RemoveCustomerFromSaleCommandHandler(
                new SaleRepository(context),
                new UnitOfWork(context))
            .Handle(
                new RemoveCustomerFromSaleCommand(sale.Id),
                CancellationToken.None);

        Assert.NotNull(removeResult);
        Assert.Null(removeResult.CustomerId);
        Assert.Null(removeResult.CustomerName);
        Assert.Null(removeResult.CustomerIdentificationNumber);
        Assert.Equal("Sale comment", removeResult.SaleComment);
    }

    [Fact]
    public async Task CustomerEdit_DoesNotChangeCompletedSaleHistorySnapshot()
    {
        await using var context = CreateContext();
        var customerRepository = new CustomerRepository(context);
        var saleRepository = new SaleRepository(context);
        var unitOfWork = new UnitOfWork(context);
        var customer = Customer.Create("Old Name", "01001", "Information");
        var sale = Sale.Create("20260825-0001");
        await customerRepository.AddAsync(customer);
        await saleRepository.AddAsync(sale);
        await unitOfWork.SaveChangesAsync();

        await new AssignCustomerToSaleCommandHandler(
                saleRepository,
                customerRepository,
                unitOfWork)
            .Handle(
                new AssignCustomerToSaleCommand(sale.Id, customer.Id),
                CancellationToken.None);
        sale.AddManualItem("Product", 1m, 10m);
        sale.Complete(
            [new SalePaymentAllocation(PaymentType.Cash, 10m)],
            DateTime.UtcNow);
        await unitOfWork.SaveChangesAsync();

        await new UpdateCustomerCommandHandler(customerRepository, unitOfWork)
            .Handle(
                new UpdateCustomerCommand(customer.Id, "New Name", "01001", "Updated"),
                CancellationToken.None);
        context.ChangeTracker.Clear();

        var readService = new SalesReadService(context, TimeProvider.System);
        var details = await new GetSaleDetailsQueryHandler(readService)
            .Handle(new GetSaleDetailsQuery(sale.Id), CancellationToken.None);
        var history = await new GetSalesHistoryQueryHandler(readService)
            .Handle(
                new GetSalesHistoryQuery(Status: SaleStatus.Completed),
                CancellationToken.None);
        var currentCustomer = await context.Customers.SingleAsync();

        Assert.NotNull(details);
        Assert.Equal("Old Name", details.CustomerName);
        Assert.Equal("Old Name", Assert.Single(history.Items).CustomerName);
        Assert.Equal("New Name", currentCustomer.Name);
    }

    [Fact]
    public async Task LegacySaleWithoutCustomerId_RemainsReadable()
    {
        await using var context = CreateContext();
        var sale = Sale.Create(
            "20260825-0001",
            customerName: "Legacy Customer",
            customerIdentificationNumber: "legacy-id",
            comment: "Legacy sale comment");
        await new SaleRepository(context).AddAsync(sale);
        await new UnitOfWork(context).SaveChangesAsync();
        context.ChangeTracker.Clear();

        var details = await new GetSaleDetailsQueryHandler(
                new SalesReadService(context, TimeProvider.System))
            .Handle(new GetSaleDetailsQuery(sale.Id), CancellationToken.None);

        Assert.NotNull(details);
        Assert.Equal("Legacy Customer", details.CustomerName);
        Assert.Equal("legacy-id", details.CustomerIdentificationNumber);
        Assert.Equal("Legacy sale comment", details.Comment);
    }

    private static StorePosDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<StorePosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new StorePosDbContext(options);
    }
}
