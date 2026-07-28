namespace Rovio.Domain.Common;

public abstract class Entity<TModel>
{
    protected Entity(Id<TModel> id)
    {
        Id = id;
        var now = DateTimeOffset.UtcNow;
        CreatedAtUtc = now;
        LastModifiedAtUtc = now;
    }

    protected Entity(Id<TModel> id, DateTimeOffset createdAtUtc, DateTimeOffset lastModifiedAtUtc)
    {
        Id = id;
        CreatedAtUtc = createdAtUtc;
        LastModifiedAtUtc = lastModifiedAtUtc;
    }

    public Id<TModel> Id { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset LastModifiedAtUtc { get; }

    public override bool Equals(object? obj)
    {
        if (obj is Entity<TModel> other)
            return other.Id == Id;
        return false;
    }

    public override int GetHashCode() => Id.GetHashCode();
}
