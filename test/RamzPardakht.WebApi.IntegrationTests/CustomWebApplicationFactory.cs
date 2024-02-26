using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Time.Testing;
using Minio;
using Moq;
using NBXplorer;
using RamzPardakht.ApplicationCore.Contracts;
using RamzPardakht.ApplicationCore.Entities;
using RamzPardakht.Infrastructure.DbContexts;
using RamzPardakht.Infrastructure.Services;
using RamzPardakht.WebApi.BackgroundServices;
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
            services.Remove(services.First(serviceDescriptor => serviceDescriptor.ServiceType == typeof(IMinioClient)));

            var minioMock = new Mock<IMinioClient>();

            services.AddTransient(_ => minioMock);
            services.AddTransient<IMinioClient>(_ => minioMock.Object);


            services.Remove(services.First(serviceDescriptor => serviceDescriptor.ServiceType == typeof(TimeProvider)));

            var fakeTimeProvider = new FakeTimeProvider(DateTimeOffset.Now) { AutoAdvanceAmount = TimeSpan.FromMilliseconds(1) };
            services.AddSingleton<TimeProvider>(fakeTimeProvider);
            services.AddSingleton(fakeTimeProvider);
            var emailSenderMock = new Mock<IEmailSender<User>>();

            services.AddTransient(_ => emailSenderMock);
            services.AddTransient<IEmailSender<User>>(_ => emailSenderMock.Object);

            services.AddMassTransitTestHarness(x =>
            {
                var entryAssembly = typeof(Program).Assembly;

                x.AddConsumers(entryAssembly);
            });

            ServiceDescriptor? bitcoinNewBlockListener = services.FirstOrDefault(x => x.ServiceType == typeof(IHostedService) && x.ImplementationType == typeof(BitcoinNewBlockListener));
            if (bitcoinNewBlockListener is not null)
                services.Remove(bitcoinNewBlockListener);

            var coinGateMock = new Mock<ICoinGateExchangeService>();

            var toRemove = services.FirstOrDefault(d => d.ServiceType == typeof(ICoinGateExchangeService));
            services.Remove(toRemove);

            services.AddTransient(_ => coinGateMock);
            services.AddTransient<ICoinGateExchangeService>(_ => coinGateMock.Object);

            var httpMocks = new Dictionary<string, MockHttpMessageHandler>();

            var explorerClientMock = new MockHttpMessageHandler();

            services.AddHttpClient(nameof(ExplorerClient)).ConfigurePrimaryHttpMessageHandler(() => explorerClientMock);
            httpMocks.Add(nameof(ExplorerClient), explorerClientMock);

            services.AddTransient<Dictionary<string, MockHttpMessageHandler>>(_ => httpMocks);

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
