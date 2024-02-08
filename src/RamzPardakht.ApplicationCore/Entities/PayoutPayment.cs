// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace RamzPardakht.ApplicationCore.Entities;

public class PayoutPayment : ITimeable
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public Payout Payout { get; set; }
    public int PayoutId { get; set; }
    public Payment Payment { get; set; }
    public int PaymentId { get; set; }
    public int? CreatedById { get; set; }
    public Guid? CreatedByTokenId { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public int? ModifiedById { get; set; }
    public Guid? ModifiedByTokenId { get; set; }
    public DateTimeOffset? ModifiedOn { get; set; }
}
