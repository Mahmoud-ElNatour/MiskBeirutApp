using MiskBeirut.Application.Dtos.Customers;
using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Enums;
using MiskBeirut.Core.Repositories;

namespace MiskBeirut.Application.Managers;

/// <summary>Back-office customer accounts and their credit/cashback ledger.</summary>
public class CustomerManager
{
    private readonly ICustomerRepository _customers;

    public CustomerManager(ICustomerRepository customers)
    {
        _customers = customers;
    }

    public async Task<IReadOnlyList<CustomerDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var customers = await _customers.GetAllAsync(cancellationToken);
        return customers.Select(ToDto).ToList();
    }

    public async Task<CustomerDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var customer = await _customers.GetByIdAsync(id, cancellationToken);
        return customer is null ? null : ToDto(customer);
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await _customers.AddAsync(new Customer
        {
            Name = request.Name,
            PhoneNumber = request.PhoneNumber,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        return ToDto(customer);
    }

    public async Task<CustomerDto> UpdateAsync(int id, UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await _customers.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Customer {id} was not found.");

        customer.Name = request.Name;
        customer.PhoneNumber = request.PhoneNumber;

        await _customers.UpdateAsync(customer, cancellationToken);
        return ToDto(customer);
    }

    public async Task<IReadOnlyList<CustomerLedgerEntryDto>> GetLedgerAsync(int customerId, CancellationToken cancellationToken = default)
    {
        var entries = await _customers.GetLedgerAsync(customerId, cancellationToken);
        return entries.Select(ToDto).ToList();
    }

    /// <summary>
    /// Adds a ledger entry after validating the sign rule: Credit entries must be negative,
    /// Cashback entries must be positive. Validated here so violations surface as clear
    /// errors instead of database check-constraint exceptions.
    /// </summary>
    public async Task<CustomerLedgerEntryDto> AddLedgerEntryAsync(CreateCustomerLedgerEntryRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Type == CustomerLedgerType.Credit && request.Amount >= 0)
            throw new InvalidOperationException("Credit ledger entries must have a negative amount.");
        if (request.Type == CustomerLedgerType.Cashback && request.Amount <= 0)
            throw new InvalidOperationException("Cashback ledger entries must have a positive amount.");

        _ = await _customers.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new InvalidOperationException($"Customer {request.CustomerId} was not found.");

        var entry = await _customers.AddLedgerEntryAsync(new CustomerLedger
        {
            Date = request.Date,
            Amount = request.Amount,
            Type = request.Type,
            Note = request.Note,
            CustomerId = request.CustomerId,
            DailyClosingId = request.DailyClosingId
        }, cancellationToken);

        return ToDto(entry);
    }

    private static CustomerDto ToDto(Customer customer) => new()
    {
        Id = customer.Id,
        Name = customer.Name,
        PhoneNumber = customer.PhoneNumber,
        Balance = customer.Balance,
        CreatedAt = customer.CreatedAt
    };

    private static CustomerLedgerEntryDto ToDto(CustomerLedger entry) => new()
    {
        Id = entry.Id,
        Date = entry.Date,
        Amount = entry.Amount,
        Type = entry.Type,
        Note = entry.Note,
        CustomerId = entry.CustomerId,
        DailyClosingId = entry.DailyClosingId
    };
}
