// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace RamzPardakht.ApplicationCore.Entities;

public class Payout : ITimeable, ISoftDeletable
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public Currency Currency { get; set; }
    public decimal NetworkFee { get; set; }
    public string ToAddress { get; set; }
    public string TransactionId { get; set; }

    public int UserId { get; set; }
    public User User { get; set; }

    public PayoutStatus Status { get; set; } = PayoutStatus.Unconfirmed;

    public List<PayoutPayment> PayoutPayments { get; set; } = new();

    public ReferenceToken CreatedByToken { get; set; }
    public int? CreatedById { get; set; }
    public Guid? CreatedByTokenId { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public int? ModifiedById { get; set; }
    public Guid? ModifiedByTokenId { get; set; }
    public DateTimeOffset? ModifiedOn { get; set; }
    public bool IsDeleted { get; set; }
    public int? DeletedById { get; set; }
    public Guid? DeletedByTokenId { get; set; }
    public DateTimeOffset? DeletedOn { get; set; }
}

public enum PayoutStatus
{
    Unconfirmed,
    Failed,
    Done

}
