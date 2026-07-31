namespace MiskBeirut.Application.Dtos.Receivers;

public sealed record SaveReceiverRequest
{
    public string Name { get; init; } = null!;
}
