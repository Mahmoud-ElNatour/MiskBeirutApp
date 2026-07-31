namespace MiskBeirut.Core.Enums;

/// <summary>
/// Stored as a string in backoffice.employee_ledger.Type.
/// Both types are stored as negative amounts; only Advance affects cash-drawer reconciliation.
/// </summary>
public enum EmployeeLedgerType
{
    Advance,
    Deduct
}
