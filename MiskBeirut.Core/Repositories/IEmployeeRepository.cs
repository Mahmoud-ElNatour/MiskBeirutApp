using MiskBeirut.Core.Entities;

namespace MiskBeirut.Core.Repositories;

/// <summary>Employees and their working records / ledger entries.</summary>
public interface IEmployeeRepository : IRepository<Employee>
{
    Task<IReadOnlyList<Employee>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<Employee?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmployeeLedger>> GetLedgerAsync(int employeeId, CancellationToken cancellationToken = default);
    Task<EmployeeLedger> AddLedgerEntryAsync(EmployeeLedger entry, CancellationToken cancellationToken = default);
    Task<EmployeeWorking?> GetWorkingRecordAsync(int employeeId, int year, int month, CancellationToken cancellationToken = default);
    Task<EmployeeWorking> AddWorkingRecordAsync(EmployeeWorking record, CancellationToken cancellationToken = default);
    Task UpdateWorkingRecordAsync(EmployeeWorking record, CancellationToken cancellationToken = default);
}
