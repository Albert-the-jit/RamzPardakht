// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using NBitcoin;
using NBXplorer;
using NBXplorer.DerivationStrategy;
using NBXplorer.Models;

namespace RamzPardakht.ApplicationCore.Contracts;

public interface IExplorerClientAdapter<TNetwork> : IAsyncDisposable, IDisposable
    where TNetwork : class, INetworkSet

{
    NBXplorerNetwork NbXplorerNetwork { get; }

    Task TrackAsync(TrackedSource trackedSource, TrackWalletRequest trackDerivationRequest = null,
        CancellationToken cancellation = default);

    Task TrackAsync(DerivationStrategyBase strategy, CancellationToken cancellation = default);
    Task<UTXOChanges> GetUTXOsAsync(TrackedSource trackedSource, CancellationToken cancellation = default);

    Task<GetFeeRateResult> GetFeeRateAsync(int blockCount, FeeRate fallbackFeeRate,
        CancellationToken cancellation = default);

    Task<BroadcastResult> BroadcastAsync(Transaction tx, CancellationToken cancellation = default);

    Task<GetBalanceResponse> GetBalanceAsync(BitcoinAddress address, CancellationToken cancellation = default);
    Task<TransactionResult> GetTransactionAsync(uint256 txId, CancellationToken cancellation = default);
    Task<NewEventBase> ListenNewBlockAsync(CancellationToken cancellation = default);

}
