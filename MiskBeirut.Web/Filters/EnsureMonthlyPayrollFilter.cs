using Microsoft.AspNetCore.Mvc.Filters;
using MiskBeirut.Application.Managers;

namespace MiskBeirut.Web.Filters;

/// <summary>
/// Applied to the Employees and Payroll controllers (<c>[TypeFilter(typeof(EnsureMonthlyPayrollFilter))]</c>
/// at the class level) — on every request to any action there, ensures the current calendar month's
/// working records exist for every active employee (creating them, with any prior month's shortfall
/// carried over, on the first request that finds one missing) before the action runs. See
/// <see cref="EmployeeManager.EnsureCurrentMonthWorkingRecordsAsync"/> for the actual logic; this is
/// just the trigger. Idempotent — a request after the first one this month is a fast no-op per
/// employee (one lookup, no write), so applying it broadly across every action in these two
/// controllers is safe.
/// </summary>
public class EnsureMonthlyPayrollFilter : IAsyncActionFilter
{
    private readonly EmployeeManager _employees;

    public EnsureMonthlyPayrollFilter(EmployeeManager employees)
    {
        _employees = employees;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        await _employees.EnsureCurrentMonthWorkingRecordsAsync(context.HttpContext.RequestAborted);
        await next();
    }
}
