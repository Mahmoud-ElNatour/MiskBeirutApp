using MiskBeirut.Application.Dtos.Receivers;
using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Repositories;

namespace MiskBeirut.Application.Managers;

/// <summary>Payees that expenses and investor withdrawals are attributed to.</summary>
public class ReceiverManager
{
    private readonly IReceiverRepository _receivers;

    public ReceiverManager(IReceiverRepository receivers)
    {
        _receivers = receivers;
    }

    public async Task<IReadOnlyList<ReceiverDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var receivers = await _receivers.GetAllAsync(cancellationToken);
        return receivers.OrderBy(r => r.Name).Select(ToDto).ToList();
    }

    public async Task<ReceiverDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var receiver = await _receivers.GetByIdAsync(id, cancellationToken);
        return receiver is null ? null : ToDto(receiver);
    }

    public async Task<ReceiverDto> CreateAsync(SaveReceiverRequest request, CancellationToken cancellationToken = default)
    {
        var receiver = await _receivers.AddAsync(new Receiver { Name = request.Name }, cancellationToken);
        return ToDto(receiver);
    }

    public async Task<ReceiverDto> UpdateAsync(int id, SaveReceiverRequest request, CancellationToken cancellationToken = default)
    {
        var receiver = await _receivers.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Receiver {id} was not found.");

        receiver.Name = request.Name;
        await _receivers.UpdateAsync(receiver, cancellationToken);
        return ToDto(receiver);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var receiver = await _receivers.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Receiver {id} was not found.");

        await _receivers.DeleteAsync(receiver, cancellationToken);
    }

    private static ReceiverDto ToDto(Receiver receiver) => new() { Id = receiver.Id, Name = receiver.Name };
}
