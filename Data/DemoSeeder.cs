using System.Text.Json;
using Neo4j.Driver;
using Nosql_Neo4j.Repositories;
namespace Nosql_Neo4j.Data;

public static class DemoSeeder
{
    public static async Task SeedAsync(Neo4jConnection connection, string contentRoot)
    {
        var data = JsonSerializer.Deserialize<SeedData>(await File.ReadAllTextAsync(Path.Combine(contentRoot, "Data", "knowledge.seed.json")), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        await using var session = connection.Session(AccessMode.Write);
        foreach (var query in new[] { "CREATE CONSTRAINT shape_id_unique IF NOT EXISTS FOR (s:Shape) REQUIRE s.id IS UNIQUE", "CREATE CONSTRAINT shape_code_unique IF NOT EXISTS FOR (s:Shape) REQUIRE s.code IS UNIQUE", "CREATE CONSTRAINT knowledge_id_unique IF NOT EXISTS FOR (k:KnowledgeItem) REQUIRE k.id IS UNIQUE", "CREATE CONSTRAINT demo_formula_id_unique IF NOT EXISTS FOR (f:Formula) REQUIRE f.id IS UNIQUE", "CREATE CONSTRAINT seed_marker_unique IF NOT EXISTS FOR (m:SeedMarker) REQUIRE m.id IS UNIQUE" })
            await (await session.RunAsync(query)).ConsumeAsync();
        var seeded = await session.ExecuteWriteAsync(async tx =>
        {
            await (await tx.RunAsync("MERGE (m:SeedMarker {id:'quadrilateral-demo-v1'}) ON CREATE SET m.done=false SET m.lock=coalesce(m.lock,0)+1")).ConsumeAsync();
            var check = await tx.RunAsync("MATCH(m:SeedMarker {id:'quadrilateral-demo-v1'}) RETURN m.done AS done");
            if ((await check.SingleAsync())["done"].As<bool>()) return false;
            check = await tx.RunAsync("MATCH(n) WHERE n:Shape OR n:KnowledgeItem OR n:Formula RETURN count(n) AS count");
            if ((await check.SingleAsync())["count"].As<long>() != 0) throw new InvalidOperationException("Seed demo chỉ chạy trên CSDL trống. Tạo DBMS/instance thử nghiệm riêng; không xóa dữ liệu đang dùng.");
            var shapeRows = data.Shapes.Select(s => (object)new Dictionary<string, object> { { "id", s.Id }, { "code", s.Code }, { "name", s.Name }, { "aliases", s.Aliases }, { "imageUrl", s.ImageUrl } }).ToArray();
            await (await tx.RunAsync("""
                UNWIND $rows AS row CREATE(s:Shape) SET s=row,s.status='PUBLISHED',s.demo=true,
                s.reviewStatus='PENDING_TEACHER_REVIEW',s.createdAt=datetime()
                """, new { rows = shapeRows })).ConsumeAsync();
            var knowledgeRows = data.Knowledge.Select(k => (object)new Dictionary<string, object> { { "id", k.Id }, { "shapeId", k.ShapeId }, { "type", k.Type }, { "title", k.Title }, { "content", k.Content }, { "expression", k.Expression }, { "variables", k.Variables }, { "conditions", k.Conditions }, { "unit", k.Unit }, { "propertyCode", k.PropertyCode }, { "sourceTitle", k.SourceTitle }, { "sourceRef", k.SourceRef } }).ToArray();
            await (await tx.RunAsync("""
                UNWIND $rows AS row MATCH(s:Shape {id:row.shapeId}) CREATE(k:KnowledgeItem)
                SET k=row,k.status='PUBLISHED',k.demo=true,k.reviewStatus='PENDING_TEACHER_REVIEW',k.createdAt=datetime()
                CREATE(s)-[:HAS_KNOWLEDGE]->(k)
                """, new { rows = knowledgeRows })).ConsumeAsync();
            // Publish equivalent demo fields/formulas for the existing core and Compare MVC.
            foreach (var shape in data.Shapes)
            {
                var own = data.Knowledge.Where(k => k.ShapeId == shape.Id).ToArray();
                var fields = new
                {
                    id = shape.Id,
                    definition = string.Join(" ", own.Where(k => k.Type == "DEFINITION").Select(k => k.Content)),
                    properties = own.Where(k => k.Type == "PROPERTY").Select(k => k.Content).ToArray(),
                    signs = own.Where(k => k.Type == "RECOGNITION").Select(k => k.Content).ToArray(),
                    examples = own.Where(k => k.Type == "EXAMPLE").Select(k => k.Content).ToArray(),
                    convention = Nosql_Neo4j.Services.ShapeService.Convention
                };
                await (await tx.RunAsync("MATCH(s:Shape {id:$id}) SET s.definition=$definition,s.properties=$properties,s.recognitionSigns=$signs,s.examples=$examples,s.convention=$convention", fields)).ConsumeAsync();
            }
            await (await tx.RunAsync("""
                UNWIND $rows AS row MATCH(s:Shape {id:row.shapeId}) CREATE(f:Formula {id:row.id})
                SET f.name=row.title,f.expression=row.expression,f.variables=row.variables,
                    f.conditions=row.conditions,f.status='PUBLISHED',f.demo=true,f.reviewStatus='PENDING_TEACHER_REVIEW'
                CREATE(s)-[:HAS_FORMULA]->(f)
                """, new
            {
                rows = data.Knowledge.Where(k => k.Type == "FORMULA").Select(k => (object)new Dictionary<string, object>{
                    {"id",k.Id},{"shapeId",k.ShapeId},{"title",k.Title},{"expression",k.Expression},
                    {"variables",string.Join("; ",k.Variables)},{"conditions",k.Conditions}}).ToArray()
            })).ConsumeAsync();
            await (await tx.RunAsync("""
                UNWIND $rows AS row MATCH(a:Shape {id:row.child}),(b:Shape {id:row.parent})
                CREATE(a)-[:IS_A {active:true}]->(b)
                """, new { rows = data.Edges.Select(e => (object)new Dictionary<string, object> { { "child", e.Child }, { "parent", e.Parent } }).ToArray() })).ConsumeAsync();
            await (await tx.RunAsync("MATCH(m:SeedMarker {id:'quadrilateral-demo-v1'}) SET m.done=true,m.createdAt=datetime()")).ConsumeAsync(); return true;
        });
        Console.WriteLine(seeded ? "Đã tạo 6 hình, 49 mục kiến thức, 6 quan hệ. DỮ LIỆU DEMO — cần giảng viên rà soát." : "Dữ liệu demo đã được khởi tạo; không ghi đè các thay đổi hiện có.");
    }
    public sealed record SeedData(SeedShape[] Shapes, SeedKnowledge[] Knowledge, SeedEdge[] Edges);
    public sealed record SeedShape(string Id, string Code, string Name, string[] Aliases, string ImageUrl);
    public sealed record SeedKnowledge(string Id, string ShapeId, string Type, string Title, string Content, string Expression, string[] Variables, string Conditions, string Unit, string PropertyCode, string SourceTitle, string SourceRef);
    public sealed record SeedEdge(string Child, string Parent);
}