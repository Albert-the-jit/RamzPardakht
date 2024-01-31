// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NBitcoin;
using NBXplorer;
using NBXplorer.Models;
using RamzPardakht.ApplicationCore.Contracts;
using RamzPardakht.ApplicationCore.Entities;
using RamzPardakht.WebApi.Hubs;
using RamzPardakht.WebApi.Models;

namespace RamzPardakht.WebApi.BackgroundServices;

public class BitcoinTransactionListener : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<BitcoinTransactionListener> _logger;

    public BitcoinTransactionListener(
        IServiceProvider serviceProvider,
        IHttpClientFactory httpClientFactory,
        ILogger<BitcoinTransactionListener> logger)
    {
        _serviceProvider = serviceProvider;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await CheckForNewlyPaidPayments(stoppingToken);
        }
        catch (Exception e) when (e is not TaskCanceledException)
        {
            _logger.LogError(e.ToString());
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

            await ExecuteAsync(stoppingToken);
        }
    }

    private async Task CheckForNewlyPaidPayments(CancellationToken stoppingToken)
    {
        var httpClient = _httpClientFactory.CreateClient(nameof(ExplorerClient));
        using var scope = _serviceProvider.CreateScope();
        var scopeServiceProvider = scope.ServiceProvider;

        var projectDbContext = scopeServiceProvider.GetRequiredService<IProjectDbContext>();
        var timeProvider = scopeServiceProvider.GetRequiredService<TimeProvider>();
        var hubContext = scopeServiceProvider.GetRequiredService<IHubContext<PaymentHub, IPaymentClient>>();
        var bitcoinWalletProvider = scopeServiceProvider.GetRequiredService<IBitcoinWalletProvider>();

        var network = new NBXplorerNetworkProvider(ChainName.Testnet).GetBTC();
        var userDerivationScheme =
            network.DerivationStrategyFactory.CreateDirectDerivationStrategy(bitcoinWalletProvider.GetMasterPublicKey());

        ExplorerClient client = new ExplorerClient(network);

        client.SetClient(httpClient);

        await client.TrackAsync(userDerivationScheme, stoppingToken);


        var session = client.CreateLongPollingNotificationSession();

        while (true)
        {
            var eventBase = await session.NextEventAsync(stoppingToken);


            if (eventBase is NewTransactionEvent transactionEvent)
            {
                if (transactionEvent.DerivationStrategy != userDerivationScheme)
                    continue;

                if (transactionEvent.TransactionData.Confirmations < 1)
                    continue;

                var address = transactionEvent.Outputs.Select(x => x.Address.ToString());

                var payments = await projectDbContext.Payments.Include(x => x.Wallet)
                    .Where(x => address.Contains(x.Wallet.Address))
                    .Where(x => x.ExpireOn > timeProvider.GetUtcNow())
                    .Where(x => x.Status != Status.Paid)
                    .ToListAsync(cancellationToken: stoppingToken);

                foreach (Payment payment in payments)
                {
                    var balanceResponse = await client.GetBalanceAsync(BitcoinAddress.Create(payment.Wallet.Address, network.NBitcoinNetwork), stoppingToken);

                    payment.PaidAmount = ((Money)balanceResponse.Confirmed).ToDecimal(MoneyUnit.BTC);
                    payment.Status = Status.Pending;

                    if (payment.PaidAmount < payment.Amount)
                    {
                        await hubContext.Clients.Group(payment.Code.ToString()).TransactionPartiallyPayed(new TransactionPayedMessageModel()
                        {
                            Code = payment.Code
                        });
                    }
                    else
                    {
                        await hubContext.Clients.Group(payment.Code.ToString()).TransactionFullyPayed(new TransactionPayedMessageModel()
                        {
                            Code = payment.Code
                        });
                    }

                    await projectDbContext.SaveChangesAsync(stoppingToken);
                }

            }

        }
    }
}

