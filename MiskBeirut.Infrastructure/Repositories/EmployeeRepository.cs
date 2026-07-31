using Microsoft.EntityFrameworkCore;
using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Enums;
using MiskBeirut.Core.Repositories;
using MiskBeirut.Infrastructure.DbContexts;

namespace MiskBeirut.Infrastructure.Repositories;

public class EmployeeRepository : Repository<Employee>, IEmployeeRepository
{
    public EmployeeRepository(MiskBeirutDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Employee>> GetActiveAsync(CancellationToken cancellationToken = default)
        => await Context.Employees
            .AsNoTracking()
            .Where(e => e.IsActive)
            .OrderBy(e => e.Name)
            .ToListAsync(cancellationToken);

    public Task<Employee?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default)
        => Context.Employees
            .Include(e => e.WorkingRecords)
            .Include(e => e.LedgerEntries)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<IReadOnlyList<EmployeeLedger>> GetLedgerAsync(int employeeId, CancellationToken cancellationToken = default)
        => await Context.EmployeeLedgers
            .AsNoTracking()
            .Where(l => l.EmployeeId == employeeId)
            .OrderBy(l => l.Date)
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Inserts the ledger entry and rolls it into that employee+month's working-record totals
    /// (creating the working record if this is its first ledger entry for the month) in the same save.
    /// </summary>
    public async Task<EmployeeLedger> AddLedgerEntryAsync(EmployeeLedger entry, CancellationToken cancellationToken = default)
    {
        var working = await Context.EmployeeWorkings.FirstOrDefaultAsync(
            w => w.EmployeeId == entry.EmployeeId && w.Year == entry.Date.Year && w.Month == entry.Date.Month,
            cancellationToken);

        if (working is null)
        {
            working = new EmployeeWorking
            {
                EmployeeId = entry.EmployeeId,
                Year = entry.Date.Year,
                Month = entry.Date.Month,
                IsWorking = true
            };
            Context.EmployeeWorkings.Add(working);
        }

        var magnitude = Math.Abs(entry.Amount);
        if (entry.Type == EmployeeLedgerType.Advance)
            working.AdvanceTotal = (working.AdvanceTotal ?? 0) + magnitude;
        else
            working.DeductionsTotal = (working.DeductionsTotal ?? 0) + magnitude;

        Context.EmployeeLedgers.Add(entry);
        await Context.SaveChangesAsync(cancellationToken);
        return entry;
    }

    public Task<EmployeeWorking?> GetWorkingRecordAsync(int employeeId, int year, int month, CancellationToken cancellationToken = default)
        => Context.EmployeeWorkings.FirstOrDefaultAsync(
            w => w.EmployeeId == employeeId && w.Year == year && w.Month == month,
            cancellationToken);

    public async Task<EmployeeWorking> AddWorkingRecordAsync(EmployeeWorking record, CancellationToken cancellationToken = default)
    {
        Context.EmployeeWorkings.Add(record);
        await Context.SaveChangesAsync(cancellationToken);
        return record;
    }

    public async Task UpdateWorkingRecordAsync(EmployeeWorking record, CancellationToken cancellationToken = default)
    {
        Context.EmployeeWorkings.Update(record);
        await Context.SaveChangesAsync(cancellationToken);
    }
}
