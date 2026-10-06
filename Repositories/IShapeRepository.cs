using Nosql_Neo4j.Models;

namespace Nosql_Neo4j.Repositories
{
    public interface IShapeRepository
    {
        Task<IReadOnlyList<ShapeSummary>> GetPublishedAsync();
        Task<IReadOnlyList<ShapeSummary>> GetDraftAsync();
        Task<ShapeDetail?> GetDraftByIdAsync(string id);
        Task<ShapeDetail?> GetPublishedByIdAsync(string id);
        Task<ShapePair?> GetDraftPairAsync(string leftId, string rightId);
        Task<ShapePair?> GetPublishedPairAsync(string leftId, string rightId);
        Task<ShapeGraph> GetDraftGraphAsync(string fromId, string toId);
        Task<ShapeGraph> GetPublishedGraphAsync(string fromId, string toId);
    }
}
