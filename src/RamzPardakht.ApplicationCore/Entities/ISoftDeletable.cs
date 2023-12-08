namespace RamzPardakht.ApplicationCore.Entities;

public interface ISoftDeletable
{
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedOn { get; set; }
}
