using DigitalBank.Application.DTOs;
using DigitalBank.Application.Interfaces;
using DigitalBank.Domain.Entities;
using DigitalBank.Domain.Exceptions;

namespace DigitalBank.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CustomerService(ICustomerRepository customerRepository, IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerDto dto, CancellationToken ct = default)
    {
        if (await _customerRepository.ExistsByEmailAsync(dto.Email, ct))
        {
            throw new InvalidOperationException($"A customer with email '{dto.Email}' already exists.");
        }

        var customer = new Customer
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            DateOfBirth = dto.DateOfBirth
        };

        await _customerRepository.AddAsync(customer, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return ToDto(customer);
    }

    public async Task<CustomerDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var customer = await _customerRepository.GetByIdAsync(id, ct);
        return customer is null ? null : ToDto(customer);
    }

    public async Task<IReadOnlyList<CustomerDto>> GetAllAsync(CancellationToken ct = default)
    {
        var customers = await _customerRepository.GetAllAsync(ct);
        return customers.Select(ToDto).ToList();
    }

    private static CustomerDto ToDto(Customer c) =>
        new(c.Id, c.FirstName, c.LastName, c.Email, c.PhoneNumber, c.DateOfBirth);
}
