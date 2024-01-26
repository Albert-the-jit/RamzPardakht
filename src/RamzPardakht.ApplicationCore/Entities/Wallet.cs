// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using RamzPardakht.ApplicationCore.Contracts;

namespace RamzPardakht.ApplicationCore.Entities;

public class Wallet : ITimeable, ISoftDeletable
{
    public int Id { get; set; }
    public string Address { get; set; }
    public WalletVersion Version { get; set; }
    public int Path { get; set; }
    public Currency Currency { get; set; }
    public List<Payment> Payments { get; set; } = new();

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

