using System.Text.Json;
using Nosql_Neo4j.Models.Data;
using Nosql_Neo4j.Repositories;
namespace Shapes.Tests;

public static class TestData
{
    private sealed record GraphFixture(ShapeRecord[] Shapes, KnowledgeRecord[] Knowledge, FixtureEdge[] Edges);
    private sealed record FixtureEdge(string Child, string Parent);
    public static KnowledgeSnapshot Snapshot()
    {
        var data = JsonSerializer.Deserialize<GraphFixture>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "knowledge.json")), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        return new(data.Shapes, data.Knowledge,
            data.Edges.Select(e => new EdgeRecord(e.Child, e.Parent)).ToArray());
    }
}
public sealed class MemoryRepository(KnowledgeSnapshot snapshot) : IKnowledgeSnapshotRepository
{ public Task<KnowledgeSnapshot> ReadPublishedAsync(CancellationToken ct) => Task.FromResult(snapshot); }