using Neo4j.Driver;
using Nosql_Neo4j.Models;
namespace Nosql_Neo4j.Repositories
{
    public sealed class ShapeRepository : IShapeRepository
    {
        private readonly IDriver _driver;
        private readonly string _database;
        public ShapeRepository(IDriver driver,  IConfiguration configuration)
        {
            _driver = driver;
            _database = configuration["Neo4j:Database"] ?? "nosql-neo4j";
        }

        public Task<IReadOnlyList<ShapeSummary>> GetPublishedAsync()
            => GetByStatusAsync("PUBLISHED");

        public Task<IReadOnlyList<ShapeSummary>> GetDraftAsync()
            => GetByStatusAsync("DRAFT");

        public Task<ShapeGraph> GetDraftGraphAsync(string fromId, string toId)
            => GetGraphByStatusAsync("DRAFT", fromId, toId);

        public Task<ShapeGraph> GetPublishedGraphAsync(string fromId, string toId)
            => GetGraphByStatusAsync("PUBLISHED", fromId, toId);

        private async Task<ShapeGraph> GetGraphByStatusAsync(string status, string fromId, string toId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fromId);
            ArgumentException.ThrowIfNullOrWhiteSpace(toId);
            await using var session = _driver.AsyncSession(
                config => config.WithDatabase(_database));

            return await session.ExecuteReadAsync(async transaction =>
            {
                var parameters = new { status, fromId, toId };
                var nodeCursor = await transaction.RunAsync("""
                    MATCH (s:Shape) WHERE s.status = $status
                    RETURN s.id AS id, s.name AS name ORDER BY id
                    """, parameters);
                var nodes = new List<ShapeSummary>();
                while (await nodeCursor.FetchAsync())
                    nodes.Add(new ShapeSummary(nodeCursor.Current["id"].As<string>(),
                        nodeCursor.Current["name"].As<string>()));

                var edgeCursor = await transaction.RunAsync("""
                    MATCH (child:Shape)-[:IS_A]->(parent:Shape)
                    WHERE child.status = $status AND parent.status = $status
                    RETURN child.id AS sourceId, parent.id AS targetId
                    ORDER BY sourceId, targetId
                    """, parameters);
                var edges = new List<ShapeEdge>();
                while (await edgeCursor.FetchAsync())
                    edges.Add(new ShapeEdge(edgeCursor.Current["sourceId"].As<string>(),
                        edgeCursor.Current["targetId"].As<string>()));

                // Giới hạn độ sâu phù hợp bộ sáu hình; chỉ duyệt các node cùng trạng thái.
                var pathCursor = await transaction.RunAsync("""
                    MATCH p = (start:Shape {id: $fromId})-[:IS_A*1..5]->(finish:Shape {id: $toId})
                    WHERE all(n IN nodes(p) WHERE n.status = $status)
                    RETURN [n IN nodes(p) | n.id] AS path ORDER BY path
                    """, parameters);
                var paths = new List<string[]>();
                while (await pathCursor.FetchAsync())
                    paths.Add(pathCursor.Current["path"].As<List<string>>().ToArray());

                return new ShapeGraph(nodes, edges, paths);
            });
        }

        public Task<ShapeDetail?> GetDraftByIdAsync(string id)
            => GetDetailByStatusAsync(id, "DRAFT");

        public Task<ShapeDetail?> GetPublishedByIdAsync(string id)
            => GetDetailByStatusAsync(id, "PUBLISHED");

        public Task<ShapePair?> GetDraftPairAsync(string leftId, string rightId)
            => GetPairByStatusAsync(leftId, rightId, "DRAFT");

        public Task<ShapePair?> GetPublishedPairAsync(string leftId, string rightId)
            => GetPairByStatusAsync(leftId, rightId, "PUBLISHED");

        private async Task<ShapePair?> GetPairByStatusAsync(string leftId, string rightId, string status)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(leftId);
            ArgumentException.ThrowIfNullOrWhiteSpace(rightId);
            if (leftId == rightId)
                throw new ArgumentException("Chọn hai hình khác nhau.", nameof(rightId));

            await using var session = _driver.AsyncSession(
                config => config.WithDatabase(_database));
            return await session.ExecuteReadAsync<ShapePair?>(async transaction =>
            {
                var left = await ReadDetailAsync(transaction, leftId, status);
                if (left is null) return null;
                var right = await ReadDetailAsync(transaction, rightId, status);
                return right is null ? null : new ShapePair(left, right);
            });
        }

        private async Task<ShapeDetail?> GetDetailByStatusAsync(string id, string status)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            await using var session = _driver.AsyncSession(
                config => config.WithDatabase(_database));

            return await session.ExecuteReadAsync<ShapeDetail?>(
                transaction => ReadDetailAsync(transaction, id, status));
        }

        private static async Task<ShapeDetail?> ReadDetailAsync(
            IAsyncQueryRunner transaction, string id, string status)
        {
                var cursor = await transaction.RunAsync("""
                    MATCH (s:Shape {id: $id})
                    WHERE s.status = $status
                    OPTIONAL MATCH (s)-[:HAS_FORMULA]->(f:Formula)
                    WHERE f.status = $status
                    WITH s, f ORDER BY f.id
                    RETURN s.id AS id, s.name AS name,
                           coalesce(s.definition, '') AS definition,
                           coalesce(s.properties, []) AS properties,
                           coalesce(s.recognitionSigns, []) AS recognitionSigns,
                           coalesce(s.examples, []) AS examples,
                           coalesce(s.sourceTitle, '') AS sourceTitle,
                           coalesce(s.sourceLocator, '') AS sourceLocator,
                           coalesce(s.reviewedBy, '') AS reviewedBy,
                           coalesce(toString(s.reviewedAt), '') AS reviewedAt,
                           coalesce(s.convention, '') AS convention,
                           collect(f {
                               .id, .name, .expression, .variables, .conditions
                           }) AS formulas
                    """, new { id, status });

                if (!await cursor.FetchAsync()) return null;
                var record = cursor.Current;
                var formulas = record["formulas"]
                    .As<List<Dictionary<string, object>>>()
                    .Select(f => new ShapeFormula(
                        f["id"].As<string>(), f["name"].As<string>(),
                        f["expression"].As<string>(), f["variables"].As<string>(),
                        f["conditions"].As<string>()))
                    .ToList();

                return new ShapeDetail(
                    record["id"].As<string>(), record["name"].As<string>(),
                    record["definition"].As<string>(),
                    record["properties"].As<List<string>>().ToArray(),
                    record["recognitionSigns"].As<List<string>>().ToArray(),
                    record["examples"].As<List<string>>().ToArray(), formulas)
                {
                    SourceTitle = record["sourceTitle"].As<string>(), SourceLocator = record["sourceLocator"].As<string>(),
                    ReviewedBy = record["reviewedBy"].As<string>(), ReviewedAt = record["reviewedAt"].As<string>(),
                    Convention = record["convention"].As<string>()
                };
        }

        private async Task<IReadOnlyList<ShapeSummary>> GetByStatusAsync(string status)
        {
            await using var session = _driver.AsyncSession(
                config => config.WithDatabase(_database));

            return await session.ExecuteReadAsync(async transaction =>
            {
                var cursor = await transaction.RunAsync("""
                MATCH (s:Shape)
                WHERE s.status = $status
                RETURN s.id AS id, s.name AS name
                ORDER BY name, id
                """,
                    new { status });

                var shapes = new List<ShapeSummary>();

                while (await cursor.FetchAsync())
                {
                    var record = cursor.Current;

                    shapes.Add(new ShapeSummary(
                        record["id"].As<string>(),
                        record["name"].As<string>()));
                }

                return (IReadOnlyList<ShapeSummary>)shapes;
            });
        }
    }
}
