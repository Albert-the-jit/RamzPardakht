using Microsoft.Extensions.DependencyInjection;
using RamzPardakht.ApplicationCore.Contracts;
using RamzPardakht.ApplicationCore.Services;

namespace RamzPardakht.ApplicationCore;

public static class ApplicationCoreSetup
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IBitcoinWalletProvider, BitcoinWalletProvider>();
        return services;
    }
}
