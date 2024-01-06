// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace RamzPardakht.ApplicationCore.Entities;

public class ReferenceToken : ITimeable, ISoftDeletable
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset ExpiresUtc { get; set; }
    public List<string> Permissions { get; set; } = new();
    public int UserId { get; set; }
    public User User { get; set; }

    public int? CreatedById { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public int? ModifiedById { get; set; }
    public DateTimeOffset? ModifiedOn { get; set; }
    public bool IsDeleted { get; set; }
    public int? DeletedById { get; set; }
    public DateTimeOffset? DeletedOn { get; set; }
}
