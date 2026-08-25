using StorePos.Application.Common.Exceptions;
using StorePos.Application.Customers.Commands.Create;
using StorePos.Application.Customers.Commands.Update;
using StorePos.Domain.Aggregates.Customer;
using StorePos.Domain.Interfaces;

namespace StorePos.Application.Tests.Customers;

public sealed class CustomerCommandHandlerTests
{
    [Fact]
    public async Task Create_PersistsNormalizedCustomerAndInformation()
    {
        var repository = new FakeCustomerRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CreateCustomerCommandHandler(repository, unitOfWork);

        var result = await handler.Handle(
            new CreateCustomerCommand("  Customer  ", "  01001  ", "  Information  "),
            CancellationToken.None);

        Assert.Equal("Customer", result.Name);
        Assert.Equal("01001", result.IdentificationNumber);
        Assert.Equal("Information", result.Information);
        Assert.Single(repository.Customers);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Create_DuplicateIdentificationNumber_ThrowsConflict()
    {
        var repository = new FakeCustomerRepository();
        await repository.AddAsync(Customer.Create("Existing", "01001"));
        var handler = new CreateCustomerCommandHandler(repository, new FakeUnitOfWork());

        await Assert.ThrowsAsync<CustomerIdentificationNumberConflictException>(() =>
            handler.Handle(
                new CreateCustomerCommand("Duplicate", " 01001 "),
                CancellationToken.None));
    }

    [Fact]
    public async Task Update_ChangesEditableCustomerFields()
    {
        var repository = new FakeCustomerRepository();
        var customer = Customer.Create("Old", "01001", "Old information");
        await repository.AddAsync(customer);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new UpdateCustomerCommandHandler(repository, unitOfWork);

        var result = await handler.Handle(
            new UpdateCustomerCommand(customer.Id, "New", "02002", "New information"),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("New", customer.Name);
        Assert.Equal("02002", customer.IdentificationNumber);
        Assert.Equal("New information", customer.Information);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public void CreateValidator_RejectsEmptyName()
    {
        var result = new CreateCustomerCommandValidator().Validate(
            new CreateCustomerCommand(" "));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "Name");
    }

    private sealed class FakeCustomerRepository : ICustomerRepository
    {
        private long _nextId = 1;
        public List<Customer> Customers { get; } = [];

        public IQueryable<Customer> Query() => Customers.AsQueryable();

        public Task<Customer?> GetByIdAsync(
            long id,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Customers.SingleOrDefault(customer => customer.Id == id));

        public Task AddAsync(
            Customer entity,
            CancellationToken cancellationToken = default)
        {
            typeof(StorePos.Domain.Base.Entity<long>)
                .GetProperty(nameof(StorePos.Domain.Base.Entity<long>.Id))!
                .SetValue(entity, _nextId++);
            Customers.Add(entity);
            return Task.CompletedTask;
        }

        public Task<bool> IdentificationNumberExistsAsync(
            string identificationNumber,
            long? excludedCustomerId = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Customers.Any(customer =>
                customer.IdentificationNumber == identificationNumber &&
                (!excludedCustomerId.HasValue || customer.Id != excludedCustomerId.Value)));

        public void Update(Customer entity)
        {
        }

        public void Remove(Customer entity)
        {
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCallCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;
            return Task.FromResult(1);
        }

        public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
        public Task CommitTransactionAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
