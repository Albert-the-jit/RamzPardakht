using Microsoft.EntityFrameworkCore;
using RamzPardakht.ApplicationCore.Entities;

namespace RamzPardakht.ApplicationCore.Contracts;

public interface IProjectDbContext
{
    DbSet<Role> Roles { get; set; }
    DbSet<User> Users { get; set; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

}
