using Nosql_Neo4j.Models;
using Nosql_Neo4j.Repositories;
namespace Nosql_Neo4j.Services;

public sealed class ShapeDiagnosticService(IShapeRepository repository) : IShapeDiagnosticService
{
    public Task<IReadOnlyList<ShapeSummary>> GetDraftAsync() => repository.GetDraftAsync();
    public Task<ShapeDetail?> GetDraftByIdAsync(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return repository.GetDraftByIdAsync(id);
    }
    public Task<ShapeGraph> GetDraftGraphAsync(string fromId, string toId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fromId);
        ArgumentException.ThrowIfNullOrWhiteSpace(toId);
        return repository.GetDraftGraphAsync(fromId, toId);
    }
}
