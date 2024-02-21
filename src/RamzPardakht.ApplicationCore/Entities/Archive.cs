// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using RamzPardakht.ApplicationCore.Contracts;

namespace RamzPardakht.ApplicationCore.Entities;

public class Archive : ITimeable, ISoftDeletable
{
    [Key]
    public Guid Id { get; set; }
    public string FileExtension { get; set; }
    public ArchiveType Type { get; set; }

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

public enum ArchiveType
{
    UnUsed,
    Internal,
    Public
}

