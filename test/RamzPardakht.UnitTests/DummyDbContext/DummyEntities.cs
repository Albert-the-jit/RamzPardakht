using System;
using RamzPardakht.ApplicationCore.Entities;

namespace RamzPardakht.UnitTests.DummyDbContext;

public class DummyEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class SoftDeletableDummyEntity : ISoftDeletable
{
    public int Id { get; set; }
    public string Name { get; set; }
    public bool IsDeleted { get; set; }
    public int? DeletedById { get; set; }
    public Guid? DeletedByTokenId { get; set; }
    public DateTimeOffset? DeletedOn { get; set; }
}

public class TimeableDummyEntity : ITimeable
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int? CreatedById { get; set; }
    public Guid? CreatedByTokenId { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public int? ModifiedById { get; set; }
    public Guid? ModifiedByTokenId { get; set; }
    public DateTimeOffset? ModifiedOn { get; set; }
}

public class TimeableAndSoftDeletableDummyEntity : ITimeable, ISoftDeletable
{
    public int Id { get; set; }
    public string Name { get; set; }
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
