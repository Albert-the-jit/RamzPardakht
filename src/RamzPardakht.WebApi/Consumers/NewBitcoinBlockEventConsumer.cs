// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NBitcoin;
using NBXplorer;
using NBXplorer.Models;
using RamzPardakht.ApplicationCore.Contracts;
using RamzPardakht.ApplicationCore.Entities;
using RamzPardakht.ApplicationCore.MessageModels;
using RamzPardakht.WebApi.Hubs;
using RamzPardakht.WebApi.Models;

namespace RamzPardakht.WebApi.Consumers;

public class NewBitcoinBlockEventConsumer : IConsumer<NewBitcoinBlockEvent>
{
    private readonly IProjectDbContext _projectDbContext;
    private readonly TimeProvider _timeProvider;
    private readonly IHubContext<PaymentHub, IPaymentClient> _hubContext;
    private readonly ExplorerClient _explorerClient;
    private readonly NBXplorerNetwork _network;
    private readonly ConsumeContext ConsumeContext;
    private readonly IConfiguration _configuration;


    public NewBitcoinBlockEventConsumer(
        IHttpClientFactory httpClientFactory,
        IProjectDbContext projectDbContext,
        TimeProvider timeProvider,
        IHubContext<PaymentHub, IPaymentClient> hubContext,
        IConfiguration configuration
        )
    {

        _projectDbContext = projectDbContext;
        _timeProvider = timeProvider;
        _hubContext = hubContext;
        _configuration = configuration;

        _network = new NBXplorerNetworkProvider(ChainName.Testnet).GetBTC();

        var httpClient = httpClientFactory.CreateClient(nameof(ExplorerClient));
        ExplorerClient client = new ExplorerClient(_network, new Uri(configuration["NBXplorer:Endpoint"]!));
        client.SetClient(httpClient);

        _explorerClient = client;
    }

    public async Task Consume(ConsumeContext<NewBitcoinBlockEvent> context)
    {
        await CheckNewPaymentsStatusAndBalance(context, context.CancellationToken);
        await CheckPendingPaymentsStatusAndBalance(context, context.CancellationToken);
        await CheckUnconfirmedPayoutTransactions(context.CancellationToken);
    }

    private async Task CheckNewPaymentsStatusAndBalance(ConsumeContext<NewBitcoinBlockEvent> context,
        CancellationToken cancellationToken)
    {
        var payments = await _projectDbContext.Payments.Include(x => x.Wallet)
            .Where(x => x.ExpireOn > _timeProvider.GetUtcNow())
            .Where(x => x.Status == Status.New || x.Status == Status.Pending)
            .ToListAsync(cancellationToken: cancellationToken);

        foreach (Payment payment in payments)
        {
            var balanceResponse =
                await _explorerClient.GetBalanceAsync(
                    BitcoinAddress.Create(payment.Wallet.Address, _network.NBitcoinNetwork),
                    cancellationToken);

            payment.PaidAmount = ((Money)balanceResponse.Confirmed).ToDecimal(MoneyUnit.BTC);

            if (payment.PaidAmount <= 0)
                continue;


            payment.Status = Status.Pending;

            if (payment.PaidAmount < payment.Amount)
            {
                await _hubContext.Clients.Group(payment.Code.ToString())
                    .TransactionPartiallyPayed(new TransactionPayedMessageModel() { Code = payment.Code });
            }
            else
            {
                await _hubContext.Clients.Group(payment.Code.ToString())
                    .TransactionFullyPayed(new TransactionPayedMessageModel() { Code = payment.Code });
            }

            await _projectDbContext.SaveChangesAsync(CancellationToken.None);

            await context.Publish(
                new PaymentStatusChanged()
                {
                    Id = payment.Id,
                    Status = payment.Status,
                    ClientRefId = payment.ClientRefId,
                    WebhookUrl = payment.WebhookUrl,
                }, cancellationToken);
        }
    }

    private async Task CheckPendingPaymentsStatusAndBalance(ConsumeContext<NewBitcoinBlockEvent> context,
        CancellationToken cancellationToken)
    {
        var payments = await _projectDbContext.Payments
            .Include(x => x.Wallet)
            .Where(x => x.Status == Status.Pending)
            .Where(x => x.ExpireOn > _timeProvider.GetUtcNow().AddMinutes(-60))
            .ToListAsync(cancellationToken: cancellationToken);

        foreach (Payment payment in payments)
        {
            var balanceResponse =
                await _explorerClient.GetBalanceAsync(
                    BitcoinAddress.Create(payment.Wallet.Address, _network.NBitcoinNetwork),
                    cancellationToken);

            payment.PaidAmount = ((Money)balanceResponse.Confirmed).ToDecimal(MoneyUnit.BTC);

            var addressTrackedSource =
                new AddressTrackedSource(BitcoinAddress.Create(payment.Wallet.Address, _network.NBitcoinNetwork));

            var utxos =
                await _explorerClient.GetUTXOsAsync(
                    addressTrackedSource,
                    cancellationToken);

            decimal confirmedPaidAmount = utxos.GetUnspentCoins(6).Sum(coin => coin.Amount.ToDecimal(MoneyUnit.BTC));

            if (confirmedPaidAmount == payment.PaidAmount)
            {
                payment.Status = payment.PaidAmount >= payment.Amount ? Status.Paid : Status.UnderPaid;
            }

            await _projectDbContext.SaveChangesAsync(CancellationToken.None);

            if (payment.Status is Status.Pending or Status.UnderPaid)
                await context.Publish(
                    new PaymentStatusChanged()
                    {
                        Id = payment.Id,
                        Status = payment.Status,
                        ClientRefId = payment.ClientRefId,
                        WebhookUrl = payment.WebhookUrl,
                    }, cancellationToken);

        }
    }

    private async Task CheckUnconfirmedPayoutTransactions(CancellationToken cancellationToken)
    {
        var payouts = await _projectDbContext.Payouts
            .Where(x => x.Status == PayoutStatus.Unconfirmed)
            .ToListAsync(cancellationToken: cancellationToken);

        foreach (Payout payout in payouts)
        {
            var transactionResult =
                await _explorerClient.GetTransactionAsync(new uint256(payout.TransactionId), cancellationToken);


            if (transactionResult is null)
                payout.Status = PayoutStatus.Failed;
            else if (transactionResult.Confirmations >= 6)
                payout.Status = PayoutStatus.Done;


            await _projectDbContext.SaveChangesAsync(CancellationToken.None);

        }
    }
}
