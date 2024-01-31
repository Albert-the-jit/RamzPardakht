// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RamzPardakht.ApplicationCore.Contracts;
using RamzPardakht.ApplicationCore.Entities;
using RamzPardakht.WebApi.Models;
using TypedSignalR.Client;

namespace RamzPardakht.WebApi.Hubs;

public class PaymentHub : Hub<IPaymentClient>, IPaymentHub
{
    private readonly IProjectDbContext _projectDbContext;

    public PaymentHub(IProjectDbContext projectDbContext)
    {
        _projectDbContext = projectDbContext;
    }

    public async Task ListenToPaymentByCode(ListenToPaymentByCodeMessageModel messageModel)
    {
        CancellationToken cancellationToken = Context.ConnectionAborted;

        var payment = await _projectDbContext.Payments
            .FirstOrDefaultAsync(x => x.Code == messageModel.Code && x.Currency != Currency.NotSelected && x.Status == Status.New, cancellationToken);

        if (payment != null)
            await Groups.AddToGroupAsync(Context.ConnectionId, messageModel.Code.ToString(), cancellationToken);
    }

    public async Task TransactionFullyPayedTest(TransactionPayedMessageModel model)
    {
        await Clients.Caller.TransactionFullyPayed(new TransactionPayedMessageModel()
        {
            Code = model.Code
        });
    }

    public async Task TransactionPartiallyPayedTest(TransactionPayedMessageModel model)
    {
        await Clients.Caller.TransactionPartiallyPayed(new TransactionPayedMessageModel() { Code = model.Code });
    }
}
[Hub]
public interface IPaymentHub
{
    Task ListenToPaymentByCode(ListenToPaymentByCodeMessageModel messageModel);
    Task TransactionFullyPayedTest(TransactionPayedMessageModel model);
    Task TransactionPartiallyPayedTest(TransactionPayedMessageModel model);
}
[Receiver]
public interface IPaymentClient
{
    Task TransactionFullyPayed(TransactionPayedMessageModel model);
    Task TransactionPartiallyPayed(TransactionPayedMessageModel model);
}

