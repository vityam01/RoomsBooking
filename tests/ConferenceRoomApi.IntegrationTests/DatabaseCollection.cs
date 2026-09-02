using Xunit;

namespace ConferenceRoomApi.IntegrationTests;

/// <summary>
/// All integration test classes share one physical Postgres database
/// (conference_rooms_test). xUnit parallelizes different test classes by default, and each
/// test's InitializeAsync truncates shared tables — running two classes concurrently would
/// let one class's reset wipe data another class's test is mid-flight on. Every integration
/// test class must carry [Collection(Name)] so xUnit serializes them instead, and they all
/// share the one CustomWebApplicationFactory instance (and thus the one running host).
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class DatabaseCollection : ICollectionFixture<CustomWebApplicationFactory>
{
    public const string Name = "Database collection";
}
