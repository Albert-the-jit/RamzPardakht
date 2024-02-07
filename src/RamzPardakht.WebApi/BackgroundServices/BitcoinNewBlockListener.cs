// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using MassTransit;
using NBitcoin;
using NBXplorer;
using NBXplorer.DerivationStrategy;
using NBXplorer.Models;
using RamzPardakht.ApplicationCore.Contracts;
using RamzPardakht.ApplicationCore.MessageModels;

namespace RamzPardakht.WebApi.BackgroundServices;

public class BitcoinNewBlockListener : BackgroundService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<BitcoinNewBlockListener> _logger;
    private readonly IBitcoinWalletProvider _bitcoinWalletProvider;
    private readonly IBus _bus;

    public BitcoinNewBlockListener(
        IHttpClientFactory httpClientFactory,
        ILogger<BitcoinNewBlockListener> logger,
        IBitcoinWalletProvider bitcoinWalletProvider, IBus bus)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _bitcoinWalletProvider = bitcoinWalletProvider;
        _bus = bus;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await NewBlockListener(stoppingToken);
        }
        catch (Exception e) when (e is not TaskCanceledException)
        {
            _logger.LogError(e.ToString());
            await Task.Delay(TimeSpan.FromMinutes(20), stoppingToken);

            await ExecuteAsync(stoppingToken);
        }
    }

    private async Task NewBlockListener(CancellationToken stoppingToken)
    {
        var network = new NBXplorerNetworkProvider(ChainName.Testnet).GetBTC();

        var httpClient = _httpClientFactory.CreateClient(nameof(ExplorerClient));
        ExplorerClient client = new ExplorerClient(network, new Uri("http://localhost:32838"));
        client.SetClient(httpClient);

        var userDerivationScheme =
            network.DerivationStrategyFactory.CreateDirectDerivationStrategy(
                _bitcoinWalletProvider.GetMasterPublicKey(), new DerivationStrategyOptions()
                {
                    ScriptPubKeyType = ScriptPubKeyType.Segwit
                });

        await client.TrackAsync(userDerivationScheme, stoppingToken);

        await _bus.Publish(new NewBitcoinBlockEvent(), stoppingToken);

        var session = await client.CreateWebsocketNotificationSessionAsync(stoppingToken);

        await session.ListenNewBlockAsync(stoppingToken);

        while (true)
        {
            var newEventBase = await session.NextEventAsync(stoppingToken);
            if (newEventBase is NewBlockEvent)
            {
                await _bus.Publish(new NewBitcoinBlockEvent(), stoppingToken);
            }

        }
    }
}

