using MediatR;
using StorePos.Application.Common.Exceptions;
using StorePos.Domain.Aggregates.Customer;
using StorePos.Domain.Interfaces;

namespace StorePos.Application.Customers.Commands.Create;

public sealed class CreateCustomerCommandHandler(
    ICustomerRepository customerRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateCustomerCommand, CreateCustomerResult>
{
    public async Task<CreateCustomerResult> Handle(
        CreateCustomerCommand request,
        CancellationToken cancellationToken)
    {
        var identificationNumber = NormalizeOptional(request.IdentificationNumber);
        if (identificationNumber is not null &&
            await customerRepository.IdentificationNumberExistsAsync(
                identificationNumber,
                cancellationToken: cancellationToken))
        {
            throw new CustomerIdentificationNumberConflictException(identificationNumber);
        }

        var customer = Customer.Create(
            request.Name,
            identificationNumber,
            request.Information);

        await customerRepository.AddAsync(customer, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateCustomerResult(
            customer.Id,
            customer.Name,
            customer.IdentificationNumber,
            customer.Information);
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
