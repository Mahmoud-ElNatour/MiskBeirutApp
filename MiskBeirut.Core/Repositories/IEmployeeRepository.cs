using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Enums;

namespace MiskBeirut.Core.Repositories;

/// <summary>Employees and their working records / ledger entries.</summary>
public interface IEmployeeRepository : IRepository<Employee>
{
    Task<IReadOnlyList<Employee>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<Employee?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmployeeLedger>> GetLedgerAsync(int employeeId, CancellationToken cancellationToken = default);
    Task<EmployeeLedger> AddLedgerEntryAsync(EmployeeLedger entry, CancellationToken cancellationToken = default);

    /// <summary>Reverses the entry's contribution to its employee+month working-record totals, then removes it.</summary>
    Task DeleteLedgerEntryAsync(EmployeeLedger entry, CancellationToken cancellationToken = default);
    Task<EmployeeWorking?> GetWorkingRecordAsync(int employeeId, int year, int month, CancellationToken cancellationToken = default);
    Task<EmployeeWorking> AddWorkingRecordAsync(EmployeeWorking record, CancellationToken cancellationToken = default);
    Task UpdateWorkingRecordAsync(EmployeeWorking record, CancellationToken cancellationToken = default);

    /// <summary>Cross-employee ledger report (Deductions & Advances control-panel page), with Employee loaded.</summary>
    Task<IReadOnlyList<EmployeeLedger>> GetLedgerByTypeAsync(EmployeeLedgerType type, int? month, int? year, CancellationToken cancellationToken = default);
}
