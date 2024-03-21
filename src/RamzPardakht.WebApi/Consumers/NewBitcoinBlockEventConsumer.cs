// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NBitcoin;
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
    private readonly ILogger<NewBitcoinBlockEventConsumer> _logger;
    private readonly IExplorerClientAdapter<Bitcoin> _explorerClientAdapter;


    public NewBitcoinBlockEventConsumer(
        IProjectDbContext projectDbContext,
        TimeProvider timeProvider,
        IHubContext<PaymentHub, IPaymentClient> hubContext,
        ILogger<NewBitcoinBlockEventConsumer> logger,
        IExplorerClientAdapter<Bitcoin> explorerClientAdapter
        )
    {

        _projectDbContext = projectDbContext;
        _timeProvider = timeProvider;
        _hubContext = hubContext;
        _logger = logger;
        _explorerClientAdapter = explorerClientAdapter;
    }

    public async Task Consume(ConsumeContext<NewBitcoinBlockEvent> context)
    {
        _logger.LogInformation("new block added");
        await CheckNewPaymentsStatusAndBalance(context, context.CancellationToken);
        await CheckPendingPaymentsStatusAndBalance(context, context.CancellationToken);
        await CheckUnconfirmedPayoutTransactions(context.CancellationToken);
    }

    private async Task CheckNewPaymentsStatusAndBalance(ConsumeContext<NewBitcoinBlockEvent> context,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("CheckNewPaymentsStatusAndBalance");

        var payments = await _projectDbContext.Payments
            .Include(x => x.Wallet)
            .Where(x => x.Currency == Currency.BTC)
            .Where(x => !string.IsNullOrEmpty(x.Wallet.Address))
            .Where(x => x.ExpireOn > _timeProvider.GetUtcNow())
            .Where(x => x.Status == Status.New || x.Status == Status.Pending)
            .ToListAsync(cancellationToken: cancellationToken);

        foreach (Payment payment in payments)
        {
            _logger.LogInformation("CheckNewPaymentsStatusAndBalance {PaymentId}", payment.Id);

            var balanceResponse =
                await _explorerClientAdapter.GetBalanceAsync(
                    BitcoinAddress.Create(payment.Wallet.Address, _explorerClientAdapter.NbXplorerNetwork.NBitcoinNetwork),
                    cancellationToken);

            payment.PaidAmount = ((Money)balanceResponse.Confirmed).ToDecimal(MoneyUnit.BTC);

            if (payment.PaidAmount <= 0)
                continue;


            payment.Status = Status.Pending;

            if (payment.PaidAmount < payment.Amount)
            {
                using var activity =
                    new ActivitySource(typeof(Startup).Assembly.FullName!).StartActivity(
                        "SignalR:Invoke:TransactionPayedMessageModel");

                activity?.AddTag("Group.Name", payment.Code.ToString());

                await _hubContext.Clients.Group(payment.Code.ToString())
                    .TransactionPartiallyPayed(new TransactionPayedMessageModel() { Code = payment.Code });
            }
            else
            {
                using var activity =
                    new ActivitySource(typeof(Startup).Assembly.FullName!).StartActivity(
                        "SignalR:Invoke:TransactionPayedMessageModel");

                activity?.AddTag("Group.Name", payment.Code.ToString());

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

        _logger.LogInformation("CheckPendingPaymentsStatusAndBalance");

        var payments = await _projectDbContext.Payments
            .Include(x => x.Wallet)
            .Where(x => x.Currency == Currency.BTC)
            .Where(x => x.Status == Status.Pending)
            .ToListAsync(cancellationToken: cancellationToken);

        foreach (Payment payment in payments)
        {
            _logger.LogInformation("CheckPendingPaymentsStatusAndBalance {PaymentId}", payment.Id);

            var balanceResponse =
                await _explorerClientAdapter.GetBalanceAsync(
                    BitcoinAddress.Create(payment.Wallet.Address, _explorerClientAdapter.NbXplorerNetwork.NBitcoinNetwork),
                    cancellationToken);

            payment.PaidAmount = ((Money)balanceResponse.Confirmed).ToDecimal(MoneyUnit.BTC);

            var addressTrackedSource =
                new AddressTrackedSource(BitcoinAddress.Create(payment.Wallet.Address, _explorerClientAdapter.NbXplorerNetwork.NBitcoinNetwork));

            var utxos =
                await _explorerClientAdapter.GetUTXOsAsync(
                    addressTrackedSource,
                    cancellationToken);

            decimal confirmedPaidAmount = utxos.GetUnspentCoins(6).Sum(coin => coin.Amount.ToDecimal(MoneyUnit.BTC));

            if (confirmedPaidAmount == payment.PaidAmount)
            {
                payment.Status = payment.PaidAmount >= payment.Amount ? Status.Paid : Status.UnderPaid;
            }

            await _projectDbContext.SaveChangesAsync(CancellationToken.None);

            if (payment.Status is Status.Paid or Status.UnderPaid)
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
        _logger.LogInformation("CheckUnconfirmedPayoutTransactions");

        var payouts = await _projectDbContext.Payouts
            .Where(x => x.Status == PayoutStatus.Unconfirmed)
            .Where(x => x.Currency == Currency.BTC)
            .ToListAsync(cancellationToken: cancellationToken);

        foreach (Payout payout in payouts)
        {

            _logger.LogInformation("CheckUnconfirmedPayoutTransactions {PayoutId}", payout.Id);

            var transactionResult =
                await _explorerClientAdapter.GetTransactionAsync(new uint256(payout.TransactionId), cancellationToken);


            if (transactionResult is null)
                payout.Status = PayoutStatus.Failed;
            else if (transactionResult.Confirmations >= 6)
                payout.Status = PayoutStatus.Done;


            await _projectDbContext.SaveChangesAsync(CancellationToken.None);

        }
    }
}
