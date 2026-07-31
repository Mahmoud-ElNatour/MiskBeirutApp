using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Repositories;
using MiskBeirut.Infrastructure.DbContexts;

namespace MiskBeirut.Infrastructure.Repositories;

public class ReceiverRepository : Repository<Receiver>, IReceiverRepository
{
    public ReceiverRepository(MiskBeirutDbContext context) : base(context)
    {
    }
}
