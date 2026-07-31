using MiskBeirut.Application.Dtos.DailyClosings;
using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Repositories;

namespace MiskBeirut.Application.Managers;

/// <summary>Line-item non-cash payments (card, transfer, ...) attached to a daily closing.</summary>
public class NonCashPaymentManager
{
    private readonly INonCashPaymentRepository _payments;

    public NonCashPaymentManager(INonCashPaymentRepository payments)
    {
        _payments = payments;
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

        return ToDto(payment);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var payment = await _payments.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Non-cash payment {id} was not found.");
        await _payments.DeleteAsync(payment, cancellationToken);
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
