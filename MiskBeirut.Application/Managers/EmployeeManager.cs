using MiskBeirut.Application.Dtos.Employees;
using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Enums;
using MiskBeirut.Core.Repositories;

namespace MiskBeirut.Application.Managers;

/// <summary>Employees and their advance/deduction ledger.</summary>
public class EmployeeManager
{
    private readonly IEmployeeRepository _employees;
    private readonly IDailyClosingRepository _dailyClosings;

    // Depends on IDailyClosingRepository directly rather than DailyClosingManager — the latter
    // already depends on EmployeeManager (it posts each closing's advances/deductions through it),
    // so going the other way would be a circular dependency. Same pattern as CustomerManager.
    public EmployeeManager(IEmployeeRepository employees, IDailyClosingRepository dailyClosings)
    {
        _employees = employees;
        _dailyClosings = dailyClosings;
    }

    /// <summary>
    /// Only Advance entries affect cash-drawer reconciliation; Deduct entries are
    /// payroll-only and never leave the drawer.
    /// </summary>
    public static bool AffectsCashDrawer(EmployeeLedgerType type) => type == EmployeeLedgerType.Advance;

    /// <summary>Every employee needs a real Base Salary to be payroll-eligible — a $0 or blank
    /// value isn't a valid "not set yet" placeholder here, it's rejected outright, on both the
    /// classic Create/Edit form and the JSON API (EmployeesController's CreateJson/UpdateJson,
    /// which had no server-side check on this at all before).</summary>
    private const string BaseSalaryRequiredMessage = "Base Salary is required and must be greater than 0.";

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

    /// <summary>
    /// Called from EnsureMonthlyPayrollFilter on every request to the Employees and Payroll pages —
    /// idempotent and cheap to call repeatedly. For the current calendar month, creates a working
    /// record for every active employee who doesn't already have one yet this month: a full 30-day
    /// month (WorkingDays/ActualWorkingDays = 30, Status "Active"), ready to be adjusted via Payroll
    /// Edit if the actual month turns out different. Already-existing records for this month —
    /// including one this same call created for a different employee moments ago — are left
    /// untouched. If the employee's *previous* month ended with a negative ActualSalary (advances/
    /// deductions outweighed what they earned — see EmployeeWorking.RecomputeSalary), that shortfall
    /// is carried into the new month as a real Deduct ledger entry (not a silent number bump), noted
    /// as such, so it's visible in their history and the Deductions &amp; Advances report. It has no
    /// DailyClosingId — a carryover isn't tied to any specific day's cash register.
    /// </summary>
    public async Task EnsureCurrentMonthWorkingRecordsAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var year = today.Year;
        var month = today.Month;
        var (prevYear, prevMonth) = month == 1 ? (year - 1, 12) : (year, month - 1);

        var employees = await _employees.GetActiveAsync(cancellationToken);

        foreach (var employee in employees)
        {
            if (await _employees.GetWorkingRecordAsync(employee.Id, year, month, cancellationToken) is not null)
                continue;

            var record = new EmployeeWorking
            {
                EmployeeId = employee.Id,
                Year = year,
                Month = month,
                Status = "Active",
                WorkingDays = 30,
                ActualWorkingDays = 30,
                BaseSalary = employee.BaseSalary,
                IsWorking = true
            };
            record.RecomputeSalary();
            await _employees.AddWorkingRecordAsync(record, cancellationToken);

            var previous = await _employees.GetWorkingRecordAsync(employee.Id, prevYear, prevMonth, cancellationToken);
            if (previous?.ActualSalary is < 0)
            {
                var shortfall = Math.Abs(previous.ActualSalary.Value);
                var previousMonthName = new DateOnly(prevYear, prevMonth, 1).ToString("MMMM yyyy");

                await AddLedgerEntryAsync(new CreateEmployeeLedgerEntryRequest
                {
                    Date = today,
                    Amount = -shortfall,
                    Type = EmployeeLedgerType.Deduct,
                    Note = $"Carried over from {previousMonthName}'s shortfall.",
                    EmployeeId = employee.Id,
                    DailyClosingId = null
                }, cancellationToken);
            }
        }
    }

    public async Task<EmployeeDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var employee = await _employees.GetByIdAsync(id, cancellationToken);
        return employee is null ? null : ToDto(employee);
    }

    public async Task<EmployeeDto> CreateAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        if (request.BaseSalary <= 0)
            throw new InvalidOperationException(BaseSalaryRequiredMessage);

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
        if (request.BaseSalary <= 0)
            throw new InvalidOperationException(BaseSalaryRequiredMessage);

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

    public async Task<EmployeeDto?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        var employees = await _employees.GetAllAsync(cancellationToken);
        var employee = employees.FirstOrDefault(e => e.UserId == userId);
        return employee is null ? null : ToDto(employee);
    }

    /// <summary>
    /// Links (or unlinks, with employeeId null) one employee to a user account. A user can have
    /// at most one employee profile, so any employee currently linked to this user is unlinked first.
    /// </summary>
    public async Task AssignUserAsync(int? employeeId, int userId, CancellationToken cancellationToken = default)
    {
        var employees = await _employees.GetAllAsync(cancellationToken);
        var currentlyAssigned = employees.FirstOrDefault(e => e.UserId == userId);
        if (currentlyAssigned is not null && currentlyAssigned.Id != employeeId)
        {
            currentlyAssigned.UserId = null;
            await _employees.UpdateAsync(currentlyAssigned, cancellationToken);
        }

        if (employeeId is null)
            return;

        var employee = await _employees.GetByIdAsync(employeeId.Value, cancellationToken)
            ?? throw new InvalidOperationException($"Employee {employeeId} was not found.");

        employee.UserId = userId;
        await _employees.UpdateAsync(employee, cancellationToken);
    }

    public async Task<IReadOnlyList<EmployeeLedgerEntryDto>> GetLedgerAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        var entries = await _employees.GetLedgerAsync(employeeId, cancellationToken);
        return entries.Select(ToDto).ToList();
    }

    /// <summary>Cross-employee report for the Control Panel's Deductions &amp; Advances page.</summary>
    public async Task<IReadOnlyList<EmployeeLedgerReportEntryDto>> GetLedgerReportAsync(EmployeeLedgerType type, int? month, int? year, CancellationToken cancellationToken = default)
    {
        var entries = await _employees.GetLedgerByTypeAsync(type, month, year, cancellationToken);
        return entries.Select(e => new EmployeeLedgerReportEntryDto
        {
            Id = e.Id,
            Date = e.Date,
            Amount = e.Amount,
            Type = e.Type,
            Note = e.Note,
            EmployeeId = e.EmployeeId,
            EmployeeName = e.Employee.Name
        }).ToList();
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

        // Only Advance entries affect cash-drawer reconciliation (AffectsCashDrawer) — a Deduct is
        // payroll-only and never leaves the drawer, so it must never touch ActualCash. An entry with
        // no DailyClosingId (e.g. a carried-over shortfall) isn't tied to any drawer either way.
        if (AffectsCashDrawer(request.Type) && request.DailyClosingId is int closingId)
            await NudgeClosingActualCashAsync(closingId, request.Amount, cancellationToken);

        return ToDto(entry);
    }

    /// <summary>
    /// ActualCash is computed once (see DailyClosingManager.ApplyComputedTotals) when a closing is
    /// created/edited via the New/Edit Close forms and stored as a plain column rather than derived
    /// live. An Advance added to an existing closing afterward — from an employee's Details page —
    /// has to nudge that same stored total by its amount (already negative, matching the sign
    /// AddLedgerEntryAsync validates), or the Sales Dashboard keeps showing pre-edit numbers. Unlike
    /// customer Credits/Cashbacks, an employee ledger entry never affects AdjustedReading — that
    /// total has no advances/deductions term (see ApplyComputedTotals). No-ops if the closing has no
    /// computed ActualCash yet (mid-way through DailyClosingManager building a brand new one).
    /// </summary>
    private async Task NudgeClosingActualCashAsync(int dailyClosingId, decimal amount, CancellationToken cancellationToken)
    {
        var closing = await _dailyClosings.GetByIdAsync(dailyClosingId, cancellationToken);
        if (closing?.ActualCash is null)
            return;

        closing.ActualCash += amount;
        await _dailyClosings.UpdateAsync(closing, cancellationToken);
    }

    public async Task DeleteLedgerEntryAsync(EmployeeLedger entry, CancellationToken cancellationToken = default)
    {
        await _employees.DeleteLedgerEntryAsync(entry, cancellationToken);
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
