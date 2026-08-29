using MiskBeirut.Application.Dtos.Employees;
using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Repositories;

namespace MiskBeirut.Application.Managers;

/// <summary>Monthly working/payroll records. ActualSalary/Total are computed from each record's own
/// BaseSalary/ActualWorkingDays/Deductions/Advance — see EmployeeWorking.RecomputeSalary. No
/// carryover-debt engine yet (a negative month doesn't roll into the next one).</summary>
public class PayrollManager
{
    private readonly IEmployeeRepository _employees;

    public PayrollManager(IEmployeeRepository employees)
    {
        _employees = employees;
    }

    public async Task<IReadOnlyList<EmployeeWorkingDto>> GetByEmployeeAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await _employees.GetWithDetailsAsync(employeeId, cancellationToken);
        return employee is null
            ? []
            : employee.WorkingRecords.OrderByDescending(w => w.Year).ThenByDescending(w => w.Month).Select(ToDto).ToList();
    }

    public async Task<EmployeeWorkingDto?> GetAsync(int employeeId, int year, int month, CancellationToken cancellationToken = default)
    {
        var record = await _employees.GetWorkingRecordAsync(employeeId, year, month, cancellationToken);
        return record is null ? null : ToDto(record);
    }

    /// <summary>Every employee's working record for one month, skipping employees with none yet.</summary>
    public async Task<IReadOnlyList<EmployeeWorkingDto>> GetAllForMonthAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        var employees = await _employees.GetAllAsync(cancellationToken);
        var results = new List<EmployeeWorkingDto>();
        foreach (var employee in employees)
        {
            var record = await _employees.GetWorkingRecordAsync(employee.Id, year, month, cancellationToken);
            if (record is not null)
                results.Add(ToDto(record));
        }
        return results;
    }

    /// <summary>
    /// ActualSalary and Total are always computed here (see EmployeeWorking.RecomputeSalary), never
    /// taken from the caller — request.ActualSalary/Total don't exist on the request type at all.
    /// This is the one place a month's record gets its Status/WorkingDays/ActualWorkingDays edited
    /// directly, so it's the authoritative point to (re)apply the formula against those plus
    /// whatever Deductions/Advance/BaseSalary totals already exist. BaseSalary is a per-month
    /// snapshot (see EmployeeWorking.BaseSalary) — a brand-new record defaults it from the
    /// employee's current Base Salary; an existing record keeps its own rate unless
    /// request.BaseSalary explicitly overrides it.
    /// </summary>
    public async Task<EmployeeWorkingDto> SaveAsync(SaveEmployeeWorkingRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _employees.GetWorkingRecordAsync(request.EmployeeId, request.Year, request.Month, cancellationToken);

        if (existing is null)
        {
            var baseSalary = request.BaseSalary;
            if (baseSalary is null)
            {
                var employee = await _employees.GetByIdAsync(request.EmployeeId, cancellationToken)
                    ?? throw new InvalidOperationException($"Employee {request.EmployeeId} was not found.");
                baseSalary = employee.BaseSalary;
            }

            var record = new EmployeeWorking
            {
                EmployeeId = request.EmployeeId,
                Year = request.Year,
                Month = request.Month,
                Status = request.Status,
                WorkingDays = request.WorkingDays,
                ActualWorkingDays = request.ActualWorkingDays,
                DeductionsTotal = request.DeductionsTotal,
                AdvanceTotal = request.AdvanceTotal,
                BaseSalary = baseSalary.Value,
                IsWorking = request.IsWorking,
                Note = request.Note
            };
            record.RecomputeSalary();

            var created = await _employees.AddWorkingRecordAsync(record, cancellationToken);
            return ToDto(created);
        }

        existing.Status = request.Status;
        existing.WorkingDays = request.WorkingDays;
        existing.ActualWorkingDays = request.ActualWorkingDays;
        // These aren't sent by every caller (e.g. the Employees page's own edit modal only ever
        // sends Status/WorkingDays/IsWorking) — a caller that omits one keeps the existing value
        // rather than silently zeroing out a total accumulated elsewhere (ledger-entry sync, etc.).
        existing.DeductionsTotal = request.DeductionsTotal ?? existing.DeductionsTotal;
        existing.AdvanceTotal = request.AdvanceTotal ?? existing.AdvanceTotal;
        existing.BaseSalary = request.BaseSalary ?? existing.BaseSalary;
        existing.IsWorking = request.IsWorking;
        existing.Note = request.Note;
        existing.RecomputeSalary();

        await _employees.UpdateWorkingRecordAsync(existing, cancellationToken);
        return ToDto(existing);
    }

    private static EmployeeWorkingDto ToDto(EmployeeWorking w) => new()
    {
        Id = w.Id,
        EmployeeId = w.EmployeeId,
        Year = w.Year,
        Month = w.Month,
        Status = w.Status,
        WorkingDays = w.WorkingDays,
        ActualWorkingDays = w.ActualWorkingDays,
        DeductionsTotal = w.DeductionsTotal,
        AdvanceTotal = w.AdvanceTotal,
        BaseSalary = w.BaseSalary,
        ActualSalary = w.ActualSalary,
        Total = w.Total,
        StartedAt = w.StartedAt,
        EndedAt = w.EndedAt,
        IsWorking = w.IsWorking,
        Note = w.Note
    };
}
