namespace RamzPardakht.ApplicationCore.Entities;

public interface ITimeable
{
    public int? CreatedById { get; set; }
    public Guid? CreatedByTokenId { get; set; }
    public DateTimeOffset CreatedOn { get; set; }

    public int? ModifiedById { get; set; }
    public Guid? ModifiedByTokenId { get; set; }
    public DateTimeOffset? ModifiedOn { get; set; }

}
