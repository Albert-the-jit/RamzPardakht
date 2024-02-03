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


    public NewBitcoinBlockEventConsumer(
        IHttpClientFactory httpClientFactory,
        IProjectDbContext projectDbContext,
        TimeProvider timeProvider,
        IHubContext<PaymentHub, IPaymentClient> hubContext
    )
    {
        _projectDbContext = projectDbContext;
        _timeProvider = timeProvider;
        _hubContext = hubContext;

        _network = new NBXplorerNetworkProvider(ChainName.Testnet).GetBTC();

        var httpClient = httpClientFactory.CreateClient(nameof(ExplorerClient));
        ExplorerClient client = new ExplorerClient(_network);
        client.SetClient(httpClient);

        _explorerClient = client;
    }

    public async Task Consume(ConsumeContext<NewBitcoinBlockEvent> context)
    {
        await CheckNewPaymentsStatusAndBalance(context, context.CancellationToken);
        await CheckPendingPaymentsStatusAndBalance(context, context.CancellationToken);
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
        var payments = await _projectDbContext.Payments.Include(x => x.Wallet)
            .Where(x => x.Status == Status.Pending)
            .Where(x => x.ExpireOn < _timeProvider.GetUtcNow())
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
                payment.Status = payment.PaidAmount <= payment.Amount ? Status.Paid : Status.UnderPaid;
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
}
