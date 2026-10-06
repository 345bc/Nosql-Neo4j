using System.Text;
using System.Text.Json;
using Neo4j.Driver;
using Nosql_Neo4j.Models;
using Nosql_Neo4j.Repositories;
namespace Nosql_Neo4j.Configuration;

public static class LocalDataCommand
{
    public static async Task SetupAsync(IDriver driver, string root)
    {
        await using var session = driver.AsyncSession(c => c.WithDatabase("nosql-neo4j"));
        foreach (var file in new[] { "schema.cypher", "seed.cypher", "seed-knowledge.cypher", "practice-schema.cypher" })
        {
            foreach (var query in SplitStatements(await File.ReadAllTextAsync(Path.Combine(root, "Data", file))))
                await (await session.RunAsync(query)).ConsumeAsync();
            Console.WriteLine("OK: " + file);
        }
        var rows = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(
            await File.ReadAllTextAsync(Path.Combine(root, "Data", "questions-draft.json")))!;
        var seed = rows.Select(r => r.ToDictionary(p => p.Key, p => (object)p.Value.GetString()!)).ToArray();
        ValidateSeed(seed);
        await session.ExecuteWriteAsync(async tx =>
        {
            await (await tx.RunAsync("""
                UNWIND $rows AS row
                MATCH (s:Shape {id: row.topicId})
                MERGE (q:Question {id: row.id})
                MERGE (v:QuestionVersion {id: row.versionId})
                ON CREATE SET v.version = 1, v.prompt = row.prompt,
                    v.optionA = row.a, v.optionB = row.b, v.optionC = row.c, v.optionD = row.d,
                    v.correctKey = row.correctKey, v.explanation = row.explanation,
                    v.status = 'DRAFT', v.reviewStatus = 'PENDING'
                MERGE (q)-[:HAS_VERSION]->(v)
                FOREACH (_ IN CASE WHEN v.status = 'DRAFT' AND NOT EXISTS { MATCH (v)-[:ABOUT]->() }
                    THEN [1] ELSE [] END | MERGE (v)-[:ABOUT]->(s))
                WITH q, v WHERE NOT EXISTS { MATCH (q)-[:CURRENT]->() }
                MERGE (q)-[:CURRENT]->(v)
                """, new { rows = seed })).ConsumeAsync();
        });
        Console.WriteLine("OK: questions-draft.json (60 câu nháp; giữ phiên bản đã tồn tại)");
        // Migration is idempotent; no deletion or regrading of existing attempts.
        await MigrateAttemptsAsync(driver);
    }

    public static async Task MigrateAttemptsAsync(IDriver driver)
    {
        await using var session = driver.AsyncSession(c => c.WithDatabase("nosql-neo4j"));
        var cursor = await session.RunAsync("MATCH (a:Attempt) WHERE a.stateJson IS NOT NULL RETURN a.stateJson AS json");
        var states = await cursor.ToListAsync(r => JsonSerializer.Deserialize<AttemptState>(r["json"].As<string>())!);
        foreach (var state in states)
            await session.ExecuteWriteAsync(tx => AttemptSnapshots.WriteAsync(tx, state));
        Console.WriteLine($"OK: snapshot đồ thị cho {states.Count} lượt cũ.");
    }

    public static void ValidateSeed(IReadOnlyList<Dictionary<string, object>> rows)
    {
        if (rows.Count != 60 || rows.Select(r => r["id"]).Distinct().Count() != 60 ||
            rows.Select(r => r["versionId"]).Distinct().Count() != 60 ||
            rows.GroupBy(r => r["topicId"]).Count() != 6 || rows.GroupBy(r => r["topicId"]).Any(g => g.Count() != 10))
            throw new InvalidOperationException("Seed cần 60 mã khác nhau, 10 câu mỗi chủ đề.");
        foreach (var r in rows)
            if (r.Values.Any(v => string.IsNullOrWhiteSpace(v.ToString())) ||
                !new[] { "A", "B", "C", "D" }.Contains(r["correctKey"]) ||
                new[] { "a", "b", "c", "d" }.Select(k => r[k].ToString()!.Trim()).Distinct().Count() != 4)
                throw new InvalidOperationException("Câu hỏi seed không hợp lệ: " + r["id"]);
    }

    public static IEnumerable<string> SplitStatements(string source)
    {
        var buffer = new StringBuilder();
        char quote = '\0';
        bool comment = false;
        for (int i = 0; i < source.Length; i++)
        {
            char ch = source[i];
            if (comment) { if (ch == '\n') { comment = false; buffer.Append(ch); } continue; }
            if (quote == '\0' && ch == '/' && i + 1 < source.Length && source[i + 1] == '/')
            { comment = true; i++; continue; }
            if (quote != '\0')
            {
                buffer.Append(ch);
                if (ch == '\\' && i + 1 < source.Length) buffer.Append(source[++i]);
                else if (ch == quote) quote = '\0';
                continue;
            }
            if (ch is '\'' or '"' or '`') quote = ch;
            if (ch == ';')
            {
                if (!string.IsNullOrWhiteSpace(buffer.ToString())) yield return buffer.ToString();
                buffer.Clear();
            }
            else buffer.Append(ch);
        }
        if (quote != '\0') throw new InvalidOperationException("Cypher có chuỗi chưa đóng.");
        if (!string.IsNullOrWhiteSpace(buffer.ToString())) yield return buffer.ToString();
    }

    public static async Task VerifyAsync(IDriver driver)
    {
        await using var session = driver.AsyncSession(c => c.WithDatabase("nosql-neo4j"));
        bool invalid = false;
        foreach (var query in SplitStatements(await File.ReadAllTextAsync("Data/verify-practice.cypher")))
        {
            var cursor = await session.RunAsync(query);
            while (await cursor.FetchAsync())
            {
                Console.WriteLine(JsonSerializer.Serialize(cursor.Current.Values));
                invalid |= cursor.Current.Keys.Any(k => k.StartsWith("invalid") || k is "cycle" or "missingPublicationReview");
            }
        }
        if (invalid) throw new InvalidOperationException("Kiểm tra dữ liệu phát hiện lỗi. Xem các dòng invalid/cycle/missingPublicationReview ở trên.");
        Console.WriteLine("PASS: kiểm tra cấu trúc dữ liệu luyện tập.");
    }
}
