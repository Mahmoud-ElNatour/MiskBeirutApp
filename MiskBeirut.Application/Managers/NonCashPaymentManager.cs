using MiskBeirut.Application.Dtos.DailyClosings;
using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Repositories;

namespace MiskBeirut.Application.Managers;

/// <summary>Line-item non-cash payments (card, transfer, ...) attached to a daily closing.</summary>
public class NonCashPaymentManager
{
    private readonly INonCashPaymentRepository _payments;
    private readonly IDailyClosingRepository _dailyClosings;

    // Depends on IDailyClosingRepository directly rather than DailyClosingManager — the latter
    // already depends on NonCashPaymentManager (it posts each closing's non-cash payments through
    // it), so going the other way would be a circular dependency.
    public NonCashPaymentManager(INonCashPaymentRepository payments, IDailyClosingRepository dailyClosings)
    {
        _payments = payments;
        _dailyClosings = dailyClosings;
    }

    public async Task<IReadOnlyList<NonCashPaymentDto>> GetByDailyClosingAsync(int dailyClosingId, CancellationToken cancellationToken = default)
    {
        var payments = await _payments.GetByDailyClosingAsync(dailyClosingId, cancellationToken);
        return payments.Select(ToDto).ToList();
    }

    public async Task<NonCashPaymentDto> AddAsync(CreateNonCashPaymentRequest request, CancellationToken cancellationToken = default)
    {
        var payment = await _payments.AddAsync(new NonCashPayment
        {
            Date = request.Date,
            Amount = request.Amount,
            PaymentMethod = request.PaymentMethod,
            Note = request.Note,
            DailyClosingId = request.DailyClosingId
        }, cancellationToken);

        await NudgeClosingActualCashAsync(request.DailyClosingId, -request.Amount, cancellationToken);

        return ToDto(payment);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var payment = await _payments.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Non-cash payment {id} was not found.");
        await _payments.DeleteAsync(payment, cancellationToken);
        await NudgeClosingActualCashAsync(payment.DailyClosingId, payment.Amount, cancellationToken);
    }

    /// <summary>
    /// ActualCash is computed once (see DailyClosingManager.ApplyComputedTotals) when a closing is
    /// created/edited via the New/Edit Close forms and stored as a plain column rather than derived
    /// live. A non-cash payment added to or removed from an existing closing afterward — from the
    /// Daily Closing Details page's per-line Add/Delete — has to nudge that same stored total by
    /// <paramref name="delta"/> (a non-cash payment subtracts from cash collected, so Add passes a
    /// negative delta and Delete passes the reversal), or the Sales Dashboard keeps showing stale
    /// numbers. No-ops if the closing has no computed ActualCash yet (mid-way through
    /// DailyClosingManager building a brand new one).
    /// </summary>
    private async Task NudgeClosingActualCashAsync(int dailyClosingId, decimal delta, CancellationToken cancellationToken)
    {
        var closing = await _dailyClosings.GetByIdAsync(dailyClosingId, cancellationToken);
        if (closing?.ActualCash is null)
            return;

        closing.ActualCash += delta;
        await _dailyClosings.UpdateAsync(closing, cancellationToken);
    }

    private static NonCashPaymentDto ToDto(NonCashPayment payment) => new()
    {
        Id = payment.Id,
        Date = payment.Date,
        Amount = payment.Amount,
        PaymentMethod = payment.PaymentMethod,
        Note = payment.Note,
        DailyClosingId = payment.DailyClosingId
    };
}
