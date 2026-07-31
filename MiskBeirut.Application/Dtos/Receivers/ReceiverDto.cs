namespace MiskBeirut.Application.Dtos.Receivers;

public sealed record ReceiverDto
{
    public int Id { get; init; }
    public string Name { get; init; } = null!;
}
