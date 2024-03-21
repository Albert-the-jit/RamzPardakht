// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using NBitcoin;
using NBXplorer;
using NBXplorer.DerivationStrategy;
using NBXplorer.Models;
using RamzPardakht.ApplicationCore.Contracts;

namespace RamzPardakht.Infrastructure.Services;

[ExcludeFromCodeCoverage(Justification = "Only call external services so its easier to mock IExplorerClientAdapter so this class will have no test coverage")]
public class ExplorerClientAdapter<TNetwork> : IExplorerClientAdapter<TNetwork>
    where TNetwork : class, INetworkSet
{
    public NBXplorerNetwork NbXplorerNetwork { get; }

    private readonly ExplorerClient _explorerClient;
    private readonly Dictionary<string, WebsocketNotificationSession> _websocketNotificationSessions = new();
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public ExplorerClientAdapter(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration
        )
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;

        var type = typeof(TNetwork);

        // ReSharper disable once HeapView.ObjectAllocation
        var i = (Activator.CreateInstance(type, true) as TNetwork)!;

        NbXplorerNetwork =
            new NBXplorerNetworkProvider(new ChainName(_configuration["NBXplorer:ChainName"]!)).GetFromCryptoCode(
                i.CryptoCode);

        var httpClient = _httpClientFactory.CreateClient(nameof(_explorerClient));
        ExplorerClient client = new ExplorerClient(NbXplorerNetwork, new Uri(_configuration["NBXplorer:Endpoint"]!));
        client.SetClient(httpClient);

        _explorerClient = client;
    }

    public Task TrackAsync(TrackedSource trackedSource, TrackWalletRequest trackDerivationRequest = null,
        CancellationToken cancellation = default)
    {
        return _explorerClient.TrackAsync(trackedSource, trackDerivationRequest, cancellation);
    }

    public Task TrackAsync(DerivationStrategyBase strategy, CancellationToken cancellation = default)
    {
        return _explorerClient.TrackAsync(strategy, cancellation);
    }

    public Task<UTXOChanges> GetUTXOsAsync(TrackedSource trackedSource, CancellationToken cancellation = default)
    {
        return _explorerClient.GetUTXOsAsync(trackedSource, cancellation);
    }

    public Task<GetFeeRateResult> GetFeeRateAsync(int blockCount, FeeRate fallbackFeeRate,
        CancellationToken cancellation = default)
    {
        return _explorerClient.GetFeeRateAsync(blockCount, fallbackFeeRate, cancellation);
    }

    public Task<BroadcastResult> BroadcastAsync(Transaction tx, CancellationToken cancellation = default)
    {
        return _explorerClient.BroadcastAsync(tx, cancellation);
    }

    public Task<GetBalanceResponse> GetBalanceAsync(BitcoinAddress address, CancellationToken cancellation = default)
    {
        return _explorerClient.GetBalanceAsync(address, cancellation);
    }

    public Task<TransactionResult> GetTransactionAsync(uint256 txId, CancellationToken cancellation = default)
    {
        return _explorerClient.GetTransactionAsync(txId, cancellation);
    }

    public async Task<NewEventBase> ListenNewBlockAsync(CancellationToken cancellation = default)
    {
        if (!_websocketNotificationSessions.TryGetValue(nameof(ListenNewBlockAsync),
                out var websocketNotificationSession))
        {
            websocketNotificationSession = await _explorerClient.CreateWebsocketNotificationSessionAsync(cancellation);
            await websocketNotificationSession.ListenNewBlockAsync(cancellation);
            _websocketNotificationSessions.AddOrReplace(nameof(ListenNewBlockAsync), websocketNotificationSession);
        }

        return await websocketNotificationSession.NextEventAsync(cancellation);
    }

    public void Dispose()
    {
        foreach (WebsocketNotificationSession websocketNotificationSession in _websocketNotificationSessions.Values)
        {
            websocketNotificationSession.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (WebsocketNotificationSession websocketNotificationSession in _websocketNotificationSessions.Values)
        {
            await websocketNotificationSession.DisposeAsync();
        }
    }
}
