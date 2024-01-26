using Microsoft.EntityFrameworkCore;
using RamzPardakht.ApplicationCore.Entities;

namespace RamzPardakht.ApplicationCore.Contracts;

public interface IProjectDbContext
{
    DbSet<ReferenceToken> ReferenceTokens { get; set; }
    DbSet<Role> Roles { get; set; }
    DbSet<User> Users { get; set; }
    DbSet<Payment> Payments { get; set; }
    DbSet<Wallet> Wallets { get; set; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

}
