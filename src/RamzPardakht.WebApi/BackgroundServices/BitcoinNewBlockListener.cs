// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using MassTransit;
using NBitcoin;
using NBXplorer.DerivationStrategy;
using NBXplorer.Models;
using RamzPardakht.ApplicationCore.Contracts;
using RamzPardakht.ApplicationCore.MessageModels;

namespace RamzPardakht.WebApi.BackgroundServices;

public class BitcoinNewBlockListener : BackgroundService
{
    private readonly ILogger<BitcoinNewBlockListener> _logger;
    private readonly IBitcoinWalletProvider _bitcoinWalletProvider;
    private readonly IBus _bus;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IHostEnvironment _hostEnvironment;

    public BitcoinNewBlockListener(
        ILogger<BitcoinNewBlockListener> logger,
        IBitcoinWalletProvider bitcoinWalletProvider,
        IBus bus,
        IServiceScopeFactory serviceScopeFactory,
        IHostEnvironment hostEnvironment
        )
    {
        _logger = logger;
        _bitcoinWalletProvider = bitcoinWalletProvider;
        _bus = bus;
        _serviceScopeFactory = serviceScopeFactory;
        _hostEnvironment = hostEnvironment;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            if(_hostEnvironment.IsProduction())
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

            await NewBlockListener(stoppingToken);
        }
        catch (Exception e) when (e is not TaskCanceledException)
        {
            _logger.LogError(e.ToString());
            await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);

            await ExecuteAsync(stoppingToken);
        }
    }

    private async Task NewBlockListener(CancellationToken stoppingToken)
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();

        IExplorerClientAdapter<Bitcoin> explorerClientAdapter =
            scope.ServiceProvider.GetRequiredService<IExplorerClientAdapter<Bitcoin>>();

        var userDerivationScheme =
            explorerClientAdapter.NbXplorerNetwork.DerivationStrategyFactory.CreateDirectDerivationStrategy(
                _bitcoinWalletProvider.GetMasterPublicKey(), new DerivationStrategyOptions()
                {
                    ScriptPubKeyType = ScriptPubKeyType.Segwit
                });
        ;

        await explorerClientAdapter.TrackAsync(userDerivationScheme, stoppingToken);

        await _bus.Publish(new NewBitcoinBlockEvent(), stoppingToken);

        while (true)
        {
            var newEventBase = await explorerClientAdapter.ListenNewBlockAsync(stoppingToken);
            if (newEventBase is not null && newEventBase is NewBlockEvent)
            {
                await _bus.Publish(new NewBitcoinBlockEvent(), stoppingToken);
            }

        }
    }
}

