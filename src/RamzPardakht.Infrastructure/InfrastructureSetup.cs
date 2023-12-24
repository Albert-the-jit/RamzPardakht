using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RamzPardakht.ApplicationCore.Common;
using RamzPardakht.ApplicationCore.Contracts;
using RamzPardakht.ApplicationCore.Entities;
using RamzPardakht.Infrastructure.DbContexts;

namespace RamzPardakht.Infrastructure;

public static class InfrastructureSetup
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {

        string? provider = configuration["Provider"];

        services.AddDbContextPool<IProjectDbContext, ProjectDbContext>(options =>
        {
            options.EnableSensitiveDataLogging();

            // Sqlite only purpose here is testing infrastructure
            _ = provider switch
            {
                "Sqlite" => options.UseSqlite($"Data Source=./mydb.db;",
                    x => x.MigrationsAssembly("RamzPardakht.SqliteMigrations")),

                "Postgresql" => options.UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection")!),

                _ => options.UseSqlite($"Data Source=./mydb.db;",
                    x => x.MigrationsAssembly("RamzPardakht.SqliteMigrations")),
            };
        });
        services.AddDbContextPool<IProjectDbContext, ProjectDbContext>((serviceProvider, optionsBuilder) =>
        {
            optionsBuilder.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));

            // Second level caching
            // optionsBuilder.AddInterceptors(serviceProvider.GetRequiredService<SecondLevelCacheInterceptor>());
        });

        // Second level caching is a query cache. The results of EF commands will be stored in the cache,so that the
        // same EF commands will retrieve their data from the cache rather than executing them against the database again.
        // https://github.com/VahidN/EFCoreSecondLevelCacheInterceptor
        // services.AddEFSecondLevelCache(options =>
        //     options.UseMemoryCacheProvider().DisableLogging(true).UseCacheKeyPrefix("EF_"));

        return services;
    }
    public static async Task InitInfrastructure(this IServiceProvider serviceProvider, IConfiguration configuration)
    {
        using var scope = serviceProvider.CreateScope();
        var dataContext = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Program");


        if (dataContext.Database.IsRelational())
            await dataContext.Database.MigrateAsync();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();



        if (!await dataContext.Roles.AnyAsync(x => x.Name == SystemConst.AdminRoleName))
        {
            var role = new Role() { Name = SystemConst.AdminRoleName };
            var roleRes =
                await roleManager.CreateAsync(role);

            if (!roleRes.Succeeded)
            {
                throw new Exception("admin role creation failed");
            }
        }

        var user = new User
        {
            EmailConfirmed = true,
            UserName = configuration["Admin:Email"],
            Email = configuration["Admin:Email"]
        };

        if (!await dataContext.Users.AnyAsync())
        {
            logger.LogInformation("no user found; creating admin user");

            var res = await userManager.CreateAsync(user, configuration["Admin:Password"]);
            if (!res.Succeeded)
            {
                throw new Exception("admin user creation failed");
            }
        }

        var adminUsers = await userManager.GetUsersInRoleAsync(SystemConst.AdminRoleName);
        if (!adminUsers.Any())
        {
            user = await userManager.FindByNameAsync(configuration["Admin:Email"]);
            var convertToAdminRes = await userManager.AddToRoleAsync(user, SystemConst.AdminRoleName);
            if (!convertToAdminRes.Succeeded)
            {
                throw new Exception("admin creation failed");
            }
        }

        await dataContext.SaveChangesAsync();


        logger.LogInformation("admin creation completed");

    }
}
