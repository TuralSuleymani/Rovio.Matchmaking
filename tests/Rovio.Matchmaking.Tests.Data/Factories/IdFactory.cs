
namespace Rovio.Matchmaking.Tests.Data.Factories;

public static class IdFactory
{
    public static Id<T> CreateNew<T>() => Id<T>.New();

    public static Id<T> Create<T>(Guid? value = null) =>
        Id<T>.Create(value ?? SampleGuid).Value;

    public static Id<T> CreateFromString<T>(string? value = null) =>
        Id<T>.Create(value ?? SampleGuidString).Value;
}
