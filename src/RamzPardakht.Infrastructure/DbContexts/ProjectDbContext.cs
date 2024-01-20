using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Internal;
using RamzPardakht.ApplicationCore.Common;
using RamzPardakht.ApplicationCore.Contracts;
using RamzPardakht.ApplicationCore.Entities;

namespace RamzPardakht.Infrastructure.DbContexts;

public class ProjectDbContext : IdentityDbContext<User, Role, int, IdentityUserClaim<int>, UserRole, IdentityUserLogin<int>, IdentityRoleClaim<int>, IdentityUserToken<int>>, IProjectDbContext
{
    public ProjectDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<ReferenceToken> ReferenceTokens { get; set; }
    public DbSet<Payment> Payments { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserRole>()
            .HasOne(userRole => userRole.Role)
            .WithMany(role => role.Users)
            .HasForeignKey(userRole => userRole.RoleId);

        modelBuilder.Entity<UserRole>()
            .HasOne(userRole => userRole.User)
            .WithMany(user => user.Roles)
            .HasForeignKey(userRole => userRole.UserId);

        modelBuilder.Entity<Payment>()
            .HasIndex(payment => payment.Code)
            .IsUnique();

        // https://stackoverflow.com/questions/63063207/ef-core-setqueryfilter-reverse-isactive-to-isdeleted-in-onmodelcreating
        foreach (var entityType in modelBuilder.Model.GetEntityTypes().Where(x => x.ClrType.IsAssignableTo(typeof(ISoftDeletable))))
        {
            var parameter = Expression.Parameter(entityType.ClrType, "p");
            var deletedCheck = Expression.Lambda(Expression.NotEqual(Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted)), Expression.Constant(true)), parameter);
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(deletedCheck);
        }

        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            // SQLite does not have proper support for DateTimeOffset via Entity Framework Core, see the limitations
            // here: https://docs.microsoft.com/en-us/ef/core/providers/sqlite/limitations#query-limitations
            // To work around this, when the Sqlite database provider is used, all model properties of type DateTimeOffset
            // use the DateTimeOffsetToBinaryConverter
            // Based on: https://github.com/aspnet/EntityFrameworkCore/issues/10784#issuecomment-415769754
            // This only supports millisecond precision, but should be sufficient for most use cases.
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var properties = entityType.ClrType.GetProperties().Where(p => p.PropertyType == typeof(DateTimeOffset)
                                                                               || p.PropertyType ==
                                                                               typeof(DateTimeOffset?));
                foreach (var property in properties.Where(x => !x.GetCustomAttributes<NotMappedAttribute>().Any()))
                {
                    modelBuilder
                        .Entity(entityType.Name)
                        .Property(property.Name)
                        .HasConversion(new DateTimeOffsetToBinaryConverter());
                }
            }
        }

        // https://stackoverflow.com/questions/63063207/ef-core-setqueryfilter-reverse-isactive-to-isdeleted-in-onmodelcreating
        foreach (var entityType in modelBuilder.Model.FindEntityTypes(typeof(ISoftDeletable)))
        {
            Expression<Func<ISoftDeletable, bool>> filterExpression = entity => !entity.IsDeleted;

            entityType.SetQueryFilter(Expression.Lambda(filterExpression.Body, filterExpression.Parameters));
        }

    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ChangeTracker.DetectChanges();

        SetEntitiesContracts(cancellationToken);

        ChangeTracker.AutoDetectChangesEnabled = false;

        var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

        ChangeTracker.AutoDetectChangesEnabled = true;

        return result;
    }

    private void SetEntitiesContracts(CancellationToken cancellationToken)
    {
        var httpContext = this.GetService<IHttpContextAccessor>().HttpContext;
        var dateTimeProvider = this.GetService<ISystemClock>();

        foreach (var entry in ChangeTracker.Entries())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entry.Entity is ISoftDeletable)
            {
                if (entry.State == EntityState.Deleted)
                {
                    entry.State = EntityState.Modified;
                    entry.CurrentValues[nameof(ISoftDeletable.IsDeleted)] = true;
                    entry.CurrentValues[nameof(ISoftDeletable.DeletedOn)] = dateTimeProvider.UtcNow;
                    if (httpContext?.User?.Identity?.IsAuthenticated == true && int.TryParse(httpContext.User.Identity.Name, out int id))
                    {
                        entry.CurrentValues[nameof(ISoftDeletable.DeletedById)] = id;
                    }
                    if (httpContext?.User?.HasClaim(claim => claim.Type == SystemConst.TokenIdClaimName) == true &&
                        Guid.TryParse(httpContext?.User?.FindFirst(claim => claim.Type == SystemConst.TokenIdClaimName)?.Value, out Guid tokenId))
                    {
                        entry.CurrentValues[nameof(ISoftDeletable.DeletedByTokenId)] = tokenId;
                    }

                }
            }

            if (entry.Entity is ITimeable)
            {
                if (entry.State == EntityState.Added)
                {
                    if (httpContext?.User?.Identity?.IsAuthenticated == true && int.TryParse(httpContext.User.Identity.Name, out int id))
                    {
                        entry.CurrentValues[nameof(ITimeable.CreatedById)] = id;
                    }

                    if (httpContext?.User?.HasClaim(claim => claim.Type == SystemConst.TokenIdClaimName) == true &&
                        Guid.TryParse(httpContext?.User?.FindFirst(claim => claim.Type == SystemConst.TokenIdClaimName)?.Value, out Guid tokenId))
                    {
                        entry.CurrentValues[nameof(ITimeable.CreatedByTokenId)] = tokenId;
                    }

                    entry.CurrentValues[nameof(ITimeable.CreatedOn)] = dateTimeProvider.UtcNow;
                }
                else if (entry.State == EntityState.Modified)
                {
                    if (httpContext?.User?.Identity?.IsAuthenticated == true && int.TryParse(httpContext.User.Identity.Name, out int id))
                    {
                        entry.CurrentValues[nameof(ITimeable.ModifiedById)] = id;
                    }

                    if (httpContext?.User?.HasClaim(claim => claim.Type == SystemConst.TokenIdClaimName) == true &&
                        Guid.TryParse(httpContext?.User?.FindFirst(claim => claim.Type == SystemConst.TokenIdClaimName)?.Value, out Guid tokenId))
                    {
                        entry.CurrentValues[nameof(ITimeable.ModifiedByTokenId)] = tokenId;
                    }

                    entry.CurrentValues[nameof(ITimeable.ModifiedOn)] = dateTimeProvider.UtcNow;
                }
            }
        }
    }

}
