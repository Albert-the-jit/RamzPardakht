using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using RamzPardakht.ApplicationCore.Contracts;
using RamzPardakht.Infrastructure.DbContexts;
using RamzPardakht.WebApi.IntegrationTests.Helpers;

namespace RamzPardakht.WebApi.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {

            #region remove Ef and setup sqlite

            var descriptor = services.SingleOrDefault(
                d => d.ServiceType ==
                     typeof(DbContextOptions<ProjectDbContext>));

            services.Remove(descriptor);

            services.AddDbContextPool<IProjectDbContext, ProjectDbContext>(options =>
            {
                options.EnableSensitiveDataLogging();
                options.UseSqlite($"Data Source=./mydb{Guid.NewGuid()}.db;",
                    x => x.MigrationsAssembly("RamzPardakht.SqliteMigrations"));
            });

            #endregion
        });

        builder.ConfigureAppConfiguration(configurationBuilder =>
            configurationBuilder.AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.Test.json")));
    }
}
