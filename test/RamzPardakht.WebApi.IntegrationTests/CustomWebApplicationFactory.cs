using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NBXplorer;
using RamzPardakht.ApplicationCore.Contracts;
using RamzPardakht.ApplicationCore.Entities;
using RamzPardakht.Infrastructure.DbContexts;
using RamzPardakht.Infrastructure.Services;
using RichardSzalay.MockHttp;

namespace RamzPardakht.WebApi.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseTestServer(options => options.AllowSynchronousIO = true);
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            var emailSenderMock = new Mock<IEmailSender<User>>();

            services.AddTransient(_ => emailSenderMock);
            services.AddTransient<IEmailSender<User>>(_ => emailSenderMock.Object);

            var coinGateMock = new Mock<ICoinGateExchangeService>();

            var toRemove = services.FirstOrDefault(d => d.ServiceType == typeof(ICoinGateExchangeService));
            services.Remove(toRemove);

            services.AddTransient(_ => coinGateMock);
            services.AddTransient<ICoinGateExchangeService>(_ => coinGateMock.Object);

            var mockHttp = new MockHttpMessageHandler();

            mockHttp.When($"http://*/v1/cryptos/*/derivations/*")
                .Respond(message =>
                {
                    var x = new HttpResponseMessage(HttpStatusCode.OK);
                    x.Content.Headers.ContentLength = 0;
                    return x;
                });

            mockHttp.When($"http://*/v1/cryptos/*/events?limit=30&longPolling=True")
                .Respond("application/json", "[]");
            mockHttp.When($"http://*/v1/cryptos/*/events?limit=30&longPolling=True&lastEventId=1")
                .Respond("application/json", "[]");

            services.AddTransient(_ => mockHttp);

            services.AddHttpClient(nameof(ExplorerClient)).ConfigurePrimaryHttpMessageHandler(() => mockHttp);
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
