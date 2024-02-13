// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using RamzPardakht.ApplicationCore.Entities;

namespace RamzPardakht.WebApi.Models;

public class PaymentReportModel
{
    public int RefId { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal UsdAmount { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset ExpireOn { get; set; }
    public Currency Currency { get; set; }
    public Status Status { get; set; }
    public Guid Code { get; set; }
    public string SuccessUrl { get; set; }
    public string CancelUrl { get; set; }
    public string? WebhookUrl { get; set; }
    public string? ClientRefId { get; set; }
    public string? PayerEmail { get; set; }
    public int UserId { get; set; }
    public DateTimeOffset CreatedOn { get; set; }

}
