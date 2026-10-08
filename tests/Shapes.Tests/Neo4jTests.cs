using Microsoft.Extensions.Options;
using Neo4j.Driver;
using Nosql_Neo4j.Repositories;
using Nosql_Neo4j.Services;
using Xunit;
namespace Shapes.Tests;

public sealed class Neo4jFactAttribute : FactAttribute
{ public Neo4jFactAttribute() { if (Environment.GetEnvironmentVariable("RUN_NEO4J_TESTS") != "1") Skip = "Cần RUN_NEO4J_TESTS=1 và Neo4j thử nghiệm riêng đã seed."; } }
[CollectionDefinition("Neo4j", DisableParallelization = true)] public class Neo4jCollection;
[Collection("Neo4j")]
public sealed class Neo4jTests
{
    private static Neo4jConnection Connection() => new(Options.Create(new Neo4jOptions
    {
        Uri = Environment.GetEnvironmentVariable("Neo4j__Uri") ?? "bolt://localhost:7687",
        Username = Environment.GetEnvironmentVariable("Neo4j__Username") ?? "neo4j",
        Password = Environment.GetEnvironmentVariable("Neo4j__Password") ?? "",
        Database = Environment.GetEnvironmentVariable("Neo4j__Database") ?? "neo4j"
    }));
    [Neo4jFact]
    public async Task RealSearchAndPublishedSnapshot()
    { await using var conn = Connection(); var snapshot = await new Neo4jShapeRepository(conn).ReadPublishedAsync(default); Assert.Equal(6, snapshot.Shapes.Length); Assert.Equal(49, snapshot.Knowledge.Length); Assert.Equal(6, snapshot.Edges.Length); var list = await new ShapeService(new Neo4jShapeRepository(conn)).ListAsync(" HÌNH  THOI ", 1, default); Assert.Equal("HINH_THOI", Assert.Single(list.Items).Id); }
    [Neo4jFact]
    public async Task RealDiamondAndDetail()
    { await using var conn = Connection(); var svc = new ShapeService(new Neo4jShapeRepository(conn)); Assert.Equal(2, (await svc.GetGraphAsync("HINH_VUONG", "TU_GIAC", default)).Paths.Length); var d = await svc.GetDetailAsync("HINH_VUONG", default); Assert.Equal(15, d!.Properties.Length); Assert.Equal(3, d.Formulas.Length); Assert.NotEmpty(d.Examples); }
    [Neo4jFact]
    public async Task DraftAndArchivedContentNeverLeaks()
    {
        await using var conn = Connection(); var id = "TEST_" + Guid.NewGuid().ToString("N");
        await using var session = conn.Session(AccessMode.Write);
        try
        {
            await (await session.RunAsync("""
                CREATE(d:Shape {id:$id,name:'SECRET DRAFT',status:'DRAFT'})
                CREATE(a:Shape {id:$archived,name:'SECRET ARCHIVED',status:'ARCHIVED'})
                CREATE(k:KnowledgeItem {id:$item,type:'PROPERTY',content:'SECRET ITEM',status:'DRAFT'})
                WITH d,a,k MATCH(s:Shape {id:'HINH_VUONG'}) CREATE(s)-[:HAS_KNOWLEDGE]->(k) CREATE(s)-[:IS_A]->(d)
                """, new { id, archived = id + "A", item = id + "K" })).ConsumeAsync();
            var repo = new Neo4jShapeRepository(conn); var snapshot = await repo.ReadPublishedAsync(default);
            Assert.DoesNotContain(snapshot.Shapes, s => s.Id.StartsWith("TEST_")); Assert.DoesNotContain(snapshot.Knowledge, k => k.Id == id + "K"); Assert.DoesNotContain(snapshot.Edges, e => e.TargetId == id);
            Assert.Null(await new ShapeService(repo).GetDetailAsync(id, default));
        }
        finally { await (await session.RunAsync("MATCH(n) WHERE n.id IN $ids DETACH DELETE n", new { ids = new[] { id, id + "A", id + "K" } })).ConsumeAsync(); }
    }
    [Neo4jFact]
    public async Task RealMvcDemoRoutesUseNeo4jAndKeepTeamPages()
    {
        await using var conn = Connection();
        await using var app = new AppFactory(new Neo4jShapeRepository(conn)); using var client = app.CreateClient();
        foreach (var url in new[] { "/", "/Shapes?q=hinh%20thoi", "/Shapes/Details/HINH_VUONG",
                     "/Shapes/Graph?fromId=HINH_VUONG&toId=TU_GIAC", "/Compare", "/Compare/Result?shape1=HINH_CHU_NHAT&shape2=HINH_THOI", "/Dev/Shapes" })
            Assert.Equal(System.Net.HttpStatusCode.OK, (await client.GetAsync(url)).StatusCode);
        var graph = System.Net.WebUtility.HtmlDecode(await client.GetStringAsync("/Shapes/Graph?fromId=HINH_VUONG&toId=TU_GIAC"));
        Assert.Contains("2 đường phân loại", graph);
        Assert.Contains("Hình vuông → Hình chữ nhật", graph); Assert.Contains("Hình vuông → Hình thoi", graph);
        var search = System.Net.WebUtility.HtmlDecode(await client.GetStringAsync("/Shapes?q=hinh%20thoi"));
        Assert.Contains("Hình thoi", search); Assert.DoesNotContain("/Shapes/Details/HINH_VUONG", search);
        var detail = System.Net.WebUtility.HtmlDecode(await client.GetStringAsync("/Shapes/Details/HINH_VUONG"));
        Assert.Contains("Dấu hiệu nhận biết", detail); Assert.Contains("Nguồn nội dung", detail);
    }

    [Neo4jFact]
    public async Task ExistingShapeAndFormulaSchemaIsSupported()
    {
        await using var conn = Connection(); await using var session = conn.Session(AccessMode.Write);
        var id = "TEST_" + Guid.NewGuid().ToString("N");
        try
        {
            await (await session.RunAsync("""
                CREATE(s:Shape {id:$id,name:'Hình core thử nghiệm',status:'PUBLISHED',definition:'Định nghĩa core',
                  properties:['Tính chất core'],recognitionSigns:['Dấu hiệu core'],examples:['Ví dụ core'],sourceLocator:'test-source'})
                CREATE(f:Formula {id:$formula,status:'PUBLISHED',name:'Công thức core',expression:'P = 4a',variables:'a: cạnh',conditions:'a > 0'})
                CREATE(s)-[:HAS_FORMULA]->(f)
                """, new { id, formula = id + "F" })).ConsumeAsync();
            var d = await new ShapeService(new Neo4jShapeRepository(conn)).GetDetailAsync(id, default);
            Assert.Contains("Định nghĩa core", d!.Definition); Assert.Contains(d.Properties, p => p.Contains("Tính chất core")); Assert.Single(d.Formulas); Assert.NotEmpty(d.Sources);
        }
        finally { await (await session.RunAsync("MATCH(n) WHERE n.id IN $ids DETACH DELETE n", new { ids = new[] { id, id + "F" } })).ConsumeAsync(); }
    }

}