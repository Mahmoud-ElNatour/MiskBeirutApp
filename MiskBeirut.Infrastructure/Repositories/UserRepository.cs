using Microsoft.EntityFrameworkCore;
using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Repositories;
using MiskBeirut.Infrastructure.DbContexts;

namespace MiskBeirut.Infrastructure.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(MiskBeirutDbContext context) : base(context)
    {
    }

    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
        => Context.Users.FirstOrDefaultAsync(u => u.UserName == username, cancellationToken);
}
