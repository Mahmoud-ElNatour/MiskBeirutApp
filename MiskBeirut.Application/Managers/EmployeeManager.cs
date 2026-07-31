using MiskBeirut.Application.Dtos.Employees;
using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Enums;
using MiskBeirut.Core.Repositories;

namespace MiskBeirut.Application.Managers;

/// <summary>Employees and their advance/deduction ledger.</summary>
public class EmployeeManager
{
    private readonly IEmployeeRepository _employees;

    public EmployeeManager(IEmployeeRepository employees)
    {
        _employees = employees;
    }

    /// <summary>
    /// Only Advance entries affect cash-drawer reconciliation; Deduct entries are
    /// payroll-only and never leave the drawer.
    /// </summary>
    public static bool AffectsCashDrawer(EmployeeLedgerType type) => type == EmployeeLedgerType.Advance;

    public async Task<IReadOnlyList<EmployeeDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var employees = await _employees.GetAllAsync(cancellationToken);
        return employees.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<EmployeeDto>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var employees = await _employees.GetActiveAsync(cancellationToken);
        return employees.Select(ToDto).ToList();
    }

    public async Task<EmployeeDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var employee = await _employees.GetByIdAsync(id, cancellationToken);
        return employee is null ? null : ToDto(employee);
    }

    public async Task<EmployeeDto> CreateAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await _employees.AddAsync(new Employee
        {
            Name = request.Name,
            PhoneNumber = request.PhoneNumber,
            Position = request.Position,
            BaseSalary = request.BaseSalary,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        return ToDto(employee);
    }

    public async Task<EmployeeDto> UpdateAsync(int id, UpdateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await _employees.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Employee {id} was not found.");

        employee.Name = request.Name;
        employee.PhoneNumber = request.PhoneNumber;
        employee.Position = request.Position;
        employee.BaseSalary = request.BaseSalary;
        employee.IsActive = request.IsActive;
        employee.UpdatedAt = DateTime.UtcNow;

        await _employees.UpdateAsync(employee, cancellationToken);
        return ToDto(employee);
    }

    public async Task<IReadOnlyList<EmployeeLedgerEntryDto>> GetLedgerAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        var entries = await _employees.GetLedgerAsync(employeeId, cancellationToken);
        return entries.Select(ToDto).ToList();
    }

    /// <summary>
    /// Adds a ledger entry. Both Advance and Deduct are stored as negative amounts,
    /// so the amount is validated (and rejected) here before it reaches the database
    /// check constraint.
    /// </summary>
    public async Task<EmployeeLedgerEntryDto> AddLedgerEntryAsync(CreateEmployeeLedgerEntryRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Amount >= 0)
            throw new InvalidOperationException("Employee ledger amounts (Advance and Deduct) must be negative.");

        _ = await _employees.GetByIdAsync(request.EmployeeId, cancellationToken)
            ?? throw new InvalidOperationException($"Employee {request.EmployeeId} was not found.");

        var entry = await _employees.AddLedgerEntryAsync(new EmployeeLedger
        {
            Date = request.Date,
            Amount = request.Amount,
            Type = request.Type,
            Note = request.Note,
            EmployeeId = request.EmployeeId,
            DailyClosingId = request.DailyClosingId
        }, cancellationToken);

        return ToDto(entry);
    }

    private static EmployeeDto ToDto(Employee employee) => new()
    {
        Id = employee.Id,
        Name = employee.Name,
        PhoneNumber = employee.PhoneNumber,
        Position = employee.Position,
        BaseSalary = employee.BaseSalary,
        IsActive = employee.IsActive,
        CreatedAt = employee.CreatedAt,
        UpdatedAt = employee.UpdatedAt,
        UserId = employee.UserId
    };

    private static EmployeeLedgerEntryDto ToDto(EmployeeLedger entry) => new()
    {
        Id = entry.Id,
        Date = entry.Date,
        Amount = entry.Amount,
        Type = entry.Type,
        Note = entry.Note,
        EmployeeId = entry.EmployeeId,
        DailyClosingId = entry.DailyClosingId
    };
}
