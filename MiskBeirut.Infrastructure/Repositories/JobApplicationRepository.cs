using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Repositories;
using MiskBeirut.Infrastructure.DbContexts;

namespace MiskBeirut.Infrastructure.Repositories;

public class JobApplicationRepository : Repository<JobApplication>, IJobApplicationRepository
{
    public JobApplicationRepository(MiskBeirutDbContext context) : base(context)
    {
    }
}
