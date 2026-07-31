namespace MiskBeirut.Core.Enums;

/// <summary>
/// Stored as a string in backoffice.investor_transactions.TransactionType.
/// Expense transactions require a ReceiverId; Withdrawal transactions do not.
/// </summary>
public enum InvestorTransactionType
{
    Withdrawal,
    Expense
}
