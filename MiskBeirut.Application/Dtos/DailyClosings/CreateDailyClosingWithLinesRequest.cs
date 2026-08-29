namespace MiskBeirut.Application.Dtos.DailyClosings;

/// <summary>
/// One atomic "Save Close" submission from the Daily Closing Create page: the header plus every
/// line-item row entered for it. <see cref="DailyClosingManager.CreateWithLinesAsync"/> creates the
/// header and every row in a single DB transaction — nothing persists unless it all succeeds.
/// Rows don't carry Date/DailyClosingId; the manager fills those in from the just-created header.
/// </summary>
public sealed record CreateDailyClosingWithLinesRequest
{
    public DateOnly Date { get; init; }
    public decimal MainReading { get; init; }
    public string? Note { get; init; }

    public IReadOnlyList<GeneralExpenseLine> GeneralExpenses { get; init; } = [];
    public IReadOnlyList<InvestorExpenseLine> InvestorExpenses { get; init; } = [];
    public IReadOnlyList<EmployeeLedgerLine> Advances { get; init; } = [];
    public IReadOnlyList<EmployeeLedgerLine> Deductions { get; init; } = [];
    public IReadOnlyList<CustomerLedgerLine> Credits { get; init; } = [];
    public IReadOnlyList<CustomerLedgerLine> Cashbacks { get; init; } = [];
    public IReadOnlyList<NonCashPaymentLine> NonCashPayments { get; init; } = [];
}

public sealed record GeneralExpenseLine
{
    public int ReceiverId { get; init; }
    public decimal Amount { get; init; }
    public string? Note { get; init; }
}

public sealed record InvestorExpenseLine
{
    public int InvestorId { get; init; }
    public int ReceiverId { get; init; }
    public decimal Amount { get; init; }
    public string? Note { get; init; }
}

/// <summary>Amount is always entered as a positive figure here — the manager applies the sign each
/// ledger type requires (both Advance and Deduct are stored negative).</summary>
public sealed record EmployeeLedgerLine
{
    public int EmployeeId { get; init; }
    public decimal Amount { get; init; }
    public string? Note { get; init; }
}

/// <summary>Amount is always entered as a positive figure here — the manager applies the sign each
/// ledger type requires (Credit negative, Cashback positive).</summary>
public sealed record CustomerLedgerLine
{
    public int CustomerId { get; init; }
    public decimal Amount { get; init; }
    public string? Note { get; init; }
}

public sealed record NonCashPaymentLine
{
    public string PaymentMethod { get; init; } = null!;
    public decimal Amount { get; init; }
    public string? Note { get; init; }
}

/// <summary>
/// One atomic "Save Changes" submission from the Daily Closing Edit page: the header plus every
/// line-item row as it should look after the edit. <see cref="DailyClosingManager.UpdateWithLinesAsync"/>
/// replaces every existing line item wholesale (delete then recreate, all in one transaction) rather
/// than diffing row-by-row — simpler, and the end state is guaranteed to match exactly what was
/// submitted. Rows carry no Id for the same reason: nothing needs to track which posted row used to
/// be which stored row.
/// </summary>
public sealed record UpdateDailyClosingWithLinesRequest
{
    public DateOnly Date { get; init; }
    public decimal MainReading { get; init; }
    public string? Note { get; init; }

    public IReadOnlyList<GeneralExpenseLine> GeneralExpenses { get; init; } = [];
    public IReadOnlyList<InvestorExpenseLine> InvestorExpenses { get; init; } = [];
    public IReadOnlyList<EmployeeLedgerLine> Advances { get; init; } = [];
    public IReadOnlyList<EmployeeLedgerLine> Deductions { get; init; } = [];
    public IReadOnlyList<CustomerLedgerLine> Credits { get; init; } = [];
    public IReadOnlyList<CustomerLedgerLine> Cashbacks { get; init; } = [];
    public IReadOnlyList<NonCashPaymentLine> NonCashPayments { get; init; } = [];
}
