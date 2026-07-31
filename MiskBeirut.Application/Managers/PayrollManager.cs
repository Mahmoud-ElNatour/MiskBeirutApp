using MiskBeirut.Application.Dtos.Employees;
using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Repositories;

namespace MiskBeirut.Application.Managers;

/// <summary>Monthly working/payroll records. No automatic salary-calculation or carryover-debt engine yet — totals are entered manually.</summary>
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

    public async Task<EmployeeWorkingDto> SaveAsync(SaveEmployeeWorkingRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _employees.GetWorkingRecordAsync(request.EmployeeId, request.Year, request.Month, cancellationToken);

        if (existing is null)
        {
            var created = await _employees.AddWorkingRecordAsync(new EmployeeWorking
            {
                EmployeeId = request.EmployeeId,
                Year = request.Year,
                Month = request.Month,
                Status = request.Status,
                WorkingDays = request.WorkingDays,
                ActualWorkingDays = request.ActualWorkingDays,
                DeductionsTotal = request.DeductionsTotal,
                AdvanceTotal = request.AdvanceTotal,
                ActualSalary = request.ActualSalary,
                Total = request.Total,
                IsWorking = request.IsWorking,
                Note = request.Note
            }, cancellationToken);
            return ToDto(created);
        }

        existing.Status = request.Status;
        existing.WorkingDays = request.WorkingDays;
        existing.ActualWorkingDays = request.ActualWorkingDays;
        existing.DeductionsTotal = request.DeductionsTotal;
        existing.AdvanceTotal = request.AdvanceTotal;
        existing.ActualSalary = request.ActualSalary;
        existing.Total = request.Total;
        existing.IsWorking = request.IsWorking;
        existing.Note = request.Note;

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
        ActualSalary = w.ActualSalary,
        Total = w.Total,
        StartedAt = w.StartedAt,
        EndedAt = w.EndedAt,
        IsWorking = w.IsWorking,
        Note = w.Note
    };
}
