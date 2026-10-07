using System.Text.Json;
using Nosql_Neo4j.Data;
using Nosql_Neo4j.Models.Data;
using Nosql_Neo4j.Repositories;
namespace Shapes.Tests;

public static class TestData
{
    public static KnowledgeSnapshot Snapshot()
    {
        var data = JsonSerializer.Deserialize<DemoSeeder.SeedData>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "knowledge.seed.json")), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        return new(data.Shapes.Select(s => new ShapeRecord(s.Id, s.Name, s.Aliases, s.ImageUrl)).ToArray(),
            data.Knowledge.Select(k => new KnowledgeRecord(k.Id, k.ShapeId, k.Type, k.Title, k.Content, k.Expression, k.Variables, k.Conditions, k.Unit, k.PropertyCode, k.SourceTitle, k.SourceRef)).ToArray(),
            data.Edges.Select(e => new EdgeRecord(e.Child, e.Parent)).ToArray());
    }
}
public sealed class MemoryRepository(KnowledgeSnapshot snapshot) : IKnowledgeSnapshotRepository
{ public Task<KnowledgeSnapshot> ReadPublishedAsync(CancellationToken ct) => Task.FromResult(snapshot); }