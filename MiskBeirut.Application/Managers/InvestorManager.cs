using MiskBeirut.Application.Dtos.Investors;
using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Enums;
using MiskBeirut.Core.Repositories;

namespace MiskBeirut.Application.Managers;

/// <summary>Investors and their withdrawal/expense transactions.</summary>
public class InvestorManager
{
    private readonly IInvestorRepository _investors;

    public InvestorManager(IInvestorRepository investors)
    {
        _investors = investors;
    }

    public async Task<IReadOnlyList<InvestorDto>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var investors = await _investors.GetActiveAsync(cancellationToken);
        return investors.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<InvestorTransactionDto>> GetTransactionsAsync(int investorId, CancellationToken cancellationToken = default)
    {
        var transactions = await _investors.GetTransactionsAsync(investorId, cancellationToken);
        return transactions.Select(ToDto).ToList();
    }

    /// <summary>
    /// Adds a transaction. Expense transactions must name a receiver; Withdrawal
    /// transactions do not. Validated here so the failure is a clear error rather
    /// than a database check-constraint exception.
    /// </summary>
    public async Task<InvestorTransactionDto> AddTransactionAsync(CreateInvestorTransactionRequest request, CancellationToken cancellationToken = default)
    {
        if (request.TransactionType == InvestorTransactionType.Expense && request.ReceiverId is null)
            throw new InvalidOperationException("Expense transactions require a ReceiverId.");

        _ = await _investors.GetByIdAsync(request.InvestorId, cancellationToken)
            ?? throw new InvalidOperationException($"Investor {request.InvestorId} was not found.");

        var transaction = await _investors.AddTransactionAsync(new InvestorTransaction
        {
            Date = request.Date,
            Amount = request.Amount,
            TransactionType = request.TransactionType,
            Note = request.Note,
            DailyClosingId = request.DailyClosingId,
            InvestorId = request.InvestorId,
            ReceiverId = request.ReceiverId
        }, cancellationToken);

        return ToDto(transaction);
    }

    private static InvestorDto ToDto(Investor investor) => new()
    {
        Id = investor.Id,
        Name = investor.Name,
        IsActive = investor.IsActive,
        CreatedAt = investor.CreatedAt
    };

    private static InvestorTransactionDto ToDto(InvestorTransaction transaction) => new()
    {
        Id = transaction.Id,
        Date = transaction.Date,
        Amount = transaction.Amount,
        TransactionType = transaction.TransactionType,
        Note = transaction.Note,
        DailyClosingId = transaction.DailyClosingId,
        InvestorId = transaction.InvestorId,
        ReceiverId = transaction.ReceiverId
    };
}
