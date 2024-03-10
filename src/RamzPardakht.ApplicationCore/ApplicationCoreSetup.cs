using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RamzPardakht.ApplicationCore.Common;
using RamzPardakht.ApplicationCore.Contracts;
using RamzPardakht.ApplicationCore.Services;

namespace RamzPardakht.ApplicationCore;

public static class ApplicationCoreSetup
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AppSettings>(configuration.GetSection(nameof(AppSettings)));

        services.AddSingleton<IBitcoinWalletProvider, BitcoinWalletProvider>();

        return services;
    }
}
