// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using MassTransit;
using RamzPardakht.ApplicationCore.MessageModels;


namespace RamzPardakht.WebApi.Consumers;

public class PaymentStatusChangedConsumer : IConsumer<PaymentStatusChanged>
{
    private readonly HttpClient _httpClient;

    public PaymentStatusChangedConsumer(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task Consume(ConsumeContext<PaymentStatusChanged> context)
    {
        var msg = context.Message;
        if (msg.WebhookUrl is not null)
            await _httpClient.PostAsJsonAsync(context.Message.WebhookUrl,
                new { RefId = msg.Id, ClientRefId = msg.ClientRefId ?? null, msg.Status });
    }
}
