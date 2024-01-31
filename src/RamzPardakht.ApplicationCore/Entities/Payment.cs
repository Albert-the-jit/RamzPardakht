// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RamzPardakht.ApplicationCore.Entities;

public class Payment : ITimeable, ISoftDeletable
{
    public int Id { get; set; }
    public decimal Amount { get; set; } = 0;
    public decimal PaidAmount { get; set; } = 0;
    public decimal UsdAmount { get; set; }
    [StringLength(200)]
    public string? Description { get; set; }
    public DateTimeOffset ExpireOn { get; set; }
    public Currency Currency { get; set; } = Currency.NotSelected;
    public Status Status { get; set; } = Status.New;
    public Guid Code { get; set; } = Guid.NewGuid();
    public string SuccessUrl { get; set; }
    public string CancelUrl { get; set; }
    public string? WebhookUrl { get; set; }
    [StringLength(50)]
    public string? ClientRefId { get; set; }
    public string? PayerEmail { get; set; }

    public int? WalletId { get; set; }
    public Wallet Wallet { get; set; }

    public int UserId { get; set; }
    public User User { get; set; }

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

/// <summary>
/// Currency iso names
/// </summary>
public enum Currency
{
    NotSelected = 0,
    BTC = 1
}

public enum Status
{
    New,
    Pending,
    UnderPaid,
    Paid,
    Canceled
}
