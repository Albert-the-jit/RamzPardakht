// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using RamzPardakht.ApplicationCore.Entities;

namespace RamzPardakht.ApplicationCore.MessageModels;

public class PaymentStatusChanged
{
    public int Id { get; set; }
    public Status Status { get; set; }
    public string? ClientRefId { get; set; }
    public string? WebhookUrl { get; set; }
}
