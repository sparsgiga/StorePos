using MediatR;
using StorePos.Application.Common.Exceptions;
using StorePos.Domain.Aggregates.Customer;
using StorePos.Domain.Interfaces;

namespace StorePos.Application.Customers.Commands.Update;

public sealed class UpdateCustomerCommandHandler(
    ICustomerRepository customerRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateCustomerCommand, UpdateCustomerResult?>
{
    public async Task<UpdateCustomerResult?> Handle(
        UpdateCustomerCommand request,
        CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByIdAsync(
            request.CustomerId,
            cancellationToken);
        if (customer is null)
        {
            return null;
        }

        var identificationNumber = NormalizeOptional(request.IdentificationNumber);
        if (identificationNumber is not null &&
            await customerRepository.IdentificationNumberExistsAsync(
                identificationNumber,
                customer.Id,
                cancellationToken))
        {
            throw new CustomerIdentificationNumberConflictException(identificationNumber);
        }

        customer.Update(request.Name, identificationNumber, request.Information);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateCustomerResult(
            customer.Id,
            customer.Name,
            customer.IdentificationNumber,
            customer.Information);
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
