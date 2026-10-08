using Neo4j.Driver;
using Nosql_Neo4j.Contracts;
using Nosql_Neo4j.Models.Data;
namespace Nosql_Neo4j.Repositories;

public sealed class Neo4jOptions
{
    public string Uri { get; set; } = "bolt://localhost:7687";
    public string Username { get; set; } = "neo4j";
    public string Password { get; set; } = "";
    public string Database { get; set; } = "nosql-neo4j";
}
public sealed class Neo4jConnection : IAsyncDisposable, IDisposable
{
    private readonly Neo4jOptions options;
    private IDriver? driver;
    private readonly Func<IDriver>? sharedDriver;
    private readonly object gate = new();
    public Neo4jConnection(Microsoft.Extensions.Options.IOptions<Neo4jOptions> settings, Func<IDriver>? sharedDriver = null) { options = settings.Value; this.sharedDriver = sharedDriver; }
    public IAsyncSession Session(AccessMode mode)
    {
        if (!System.Uri.TryCreate(options.Uri, UriKind.Absolute, out var endpoint) ||
            endpoint.Scheme is not ("bolt" or "bolt+s" or "bolt+ssc" or "neo4j" or "neo4j+s" or "neo4j+ssc") ||
            string.IsNullOrWhiteSpace(options.Database) || string.IsNullOrWhiteSpace(options.Username))
            throw new DatabaseUnavailableException("Cấu hình Neo4j không hợp lệ.");
        if (string.IsNullOrWhiteSpace(options.Password)) throw new DatabaseUnavailableException("Chưa cấu hình mật khẩu Neo4j.");
        lock (gate)
        {
            driver ??= sharedDriver is not null ? sharedDriver() : GraphDatabase.Driver(options.Uri, AuthTokens.Basic(options.Username, options.Password),
                o => o.WithConnectionTimeout(TimeSpan.FromSeconds(5)).WithMaxTransactionRetryTime(TimeSpan.FromSeconds(5)));
        }
        return driver.AsyncSession(o => o.WithDatabase(options.Database).WithDefaultAccessMode(mode));
    }
    public async ValueTask DisposeAsync() { if (sharedDriver is null && driver is not null) await driver.DisposeAsync(); }
    public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();
}
public sealed class Neo4jShapeRepository(Neo4jConnection connection) : IKnowledgeSnapshotRepository
{
    // Each response uses one read transaction. DRAFT/ARCHIVED shapes and items never enter its snapshot.
    public async Task<KnowledgeSnapshot> ReadPublishedAsync(CancellationToken ct)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            await using var session = connection.Session(AccessMode.Read);
            return await session.ExecuteReadAsync(async tx =>
            {
                var cursor = await tx.RunAsync("""
                    MATCH (s:Shape) WHERE s.status=$status
                    RETURN coalesce(s.id,s.code) AS id,s.name AS name,coalesce(s.aliases,[]) AS aliases,
                      coalesce(s.imageUrl,'') AS imageUrl ORDER BY id LIMIT 101
                    """, new { status = "DRAFT" });
                var shapes = await cursor.ToListAsync(r => new ShapeRecord(ShapeIds.Canonical(r["id"].As<string>()), r["name"].As<string>(), r["aliases"].As<List<string>>().ToArray(), SafeImage(r["imageUrl"].As<string>()) ?? ShapeIds.Illustration(r["id"].As<string>())));
                if (shapes.Count > 100) throw new DomainValidationException("GRAPH_LIMIT", "Tối đa 100 hình.", new Dictionary<string, string[]> { { "", ["Dữ liệu vượt giới hạn 100 hình."] } });
                ct.ThrowIfCancellationRequested();
                cursor = await tx.RunAsync("""
                    MATCH (s:Shape)-[:HAS_KNOWLEDGE]->(k:KnowledgeItem)
                    WHERE s.status=$status AND k.status=$status
                    RETURN k.id AS id,coalesce(s.id,s.code) AS shapeId,k.type AS type,
                      coalesce(k.title,'') AS title,coalesce(k.content,'') AS content,
                      coalesce(k.expression,'') AS expression,coalesce(k.variables,[]) AS variables,
                      coalesce(k.conditions,'') AS conditions,coalesce(k.unit,'') AS unit,
                      coalesce(k.propertyCode,'') AS propertyCode,coalesce(k.sourceTitle,'Nguồn nội dung') AS sourceTitle,
                      coalesce(k.sourceLocator,'') AS sourceLocator ORDER BY id LIMIT 10001
                    """, new { status = "PUBLISHED" });
                var knowledge = await cursor.ToListAsync(r => new KnowledgeRecord(r["id"].As<string>(), ShapeIds.Canonical(r["shapeId"].As<string>()), r["type"].As<string>(), r["title"].As<string>(), r["content"].As<string>(), r["expression"].As<string>(), r["variables"].As<List<string>>().ToArray(), r["conditions"].As<string>(), r["unit"].As<string>(), r["propertyCode"].As<string>(), r["sourceTitle"].As<string>(), r["sourceLocator"].As<string>()));
                // Existing core schema stores knowledge on Shape and uses HAS_FORMULA.
                // Never use this fallback for shapes that have KnowledgeItem links: a DRAFT item
                // must not reappear through an old published copy on the Shape node.
                cursor = await tx.RunAsync("""
                    MATCH(s:Shape) WHERE s.status=$status
                      AND NOT EXISTS { MATCH(s)-[:HAS_KNOWLEDGE]->(:KnowledgeItem) }
                    OPTIONAL MATCH(s)-[:HAS_FORMULA]->(f:Formula) WHERE f.status=$status
                    RETURN coalesce(s.id,s.code) AS id,coalesce(s.definition,'') AS definition,
                      coalesce(s.properties,[]) AS properties,coalesce(s.recognitionSigns,[]) AS signs,
                      coalesce(s.examples,[]) AS examples,coalesce(s.convention,'') AS conditions,
                      coalesce(s.sourceTitle,'Nguồn nội dung') AS sourceTitle,coalesce(s.sourceLocator,'') AS sourceLocator,
                      collect(f{.id,.name,.expression,.variables,.conditions}) AS formulas
                    """, new { status = "PUBLISHED" });
                foreach (var row in await cursor.ToListAsync())
                {
                    var owner = ShapeIds.Canonical(row["id"].As<string>());
                    var conditions = row["conditions"].As<string>();
                    void Add(string type, string[] texts)
                    {
                        for (var i = 0; i < texts.Length; i++)
                            if (!string.IsNullOrWhiteSpace(texts[i])) knowledge.Add(new KnowledgeRecord(
                                $"LEGACY_{owner}_{type}_{i}", owner, type, type, texts[i], "", [], conditions, "", "",
                                row["sourceTitle"].As<string>(), row["sourceLocator"].As<string>()));
                    }
                    Add("DEFINITION", [row["definition"].As<string>()]);
                    Add("PROPERTY", row["properties"].As<List<string>>().ToArray());
                    Add("RECOGNITION", row["signs"].As<List<string>>().ToArray());
                    Add("EXAMPLE", row["examples"].As<List<string>>().ToArray());
                    foreach (var formula in row["formulas"].As<List<Dictionary<string, object>>>())
                    {
                        string Value(string key) => formula.TryGetValue(key, out var v) ? v?.ToString() ?? "" : "";
                        knowledge.Add(new KnowledgeRecord(Value("id"), owner, "FORMULA", Value("name"), Value("expression"),
                            Value("expression"), [Value("variables")], Value("conditions"), "", "",
                            row["sourceTitle"].As<string>(), row["sourceLocator"].As<string>()));
                    }
                }
                if (knowledge.Count > 10000) throw new DatabaseUnavailableException("Vượt giới hạn đọc dữ liệu.");
                ct.ThrowIfCancellationRequested();
                cursor = await tx.RunAsync("""
                    MATCH (a:Shape)-[r:IS_A]->(b:Shape)
                    WHERE a.status=$status AND b.status=$status AND coalesce(r.active,true)
                    RETURN DISTINCT coalesce(a.id,a.code) AS child,coalesce(b.id,b.code) AS parent
                    ORDER BY child,parent
                    """, new { status = "PUBLISHED" });
                var edges = await cursor.ToListAsync(r => new EdgeRecord(ShapeIds.Canonical(r["child"].As<string>()), ShapeIds.Canonical(r["parent"].As<string>())));
                return new KnowledgeSnapshot(shapes.ToArray(), knowledge.ToArray(), edges.ToArray());
            }, o => o.WithTimeout(TimeSpan.FromSeconds(8)));
        }
        catch (Neo4jException ex) { throw new DatabaseUnavailableException("Không thể đọc Neo4j.", ex); }
    }
    private static string? SafeImage(string url) => url.StartsWith("/images/", StringComparison.Ordinal) && !url.Contains("..") && !url.Contains('\\') ? url : null;
}