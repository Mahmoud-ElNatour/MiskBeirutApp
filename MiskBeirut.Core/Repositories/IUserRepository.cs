using MiskBeirut.Core.Entities;

namespace MiskBeirut.Core.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
}
