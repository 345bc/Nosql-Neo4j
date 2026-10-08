using System.Text;
using System.Text.Json;
using Neo4j.Driver;
using Nosql_Neo4j.Models;
using Nosql_Neo4j.Repositories;
namespace Nosql_Neo4j.Configuration;

public static class LocalDataCommand
{
    public static async Task RunMigrationAsync(IDriver driver, string root, IConfiguration configuration)
    {
        var file = Path.Combine(root, "migration", "seed-all.cypher");
        var queries = SplitStatements(await File.ReadAllTextAsync(file)).ToArray();
        await using var session = driver.AsyncSession(c => c.WithDatabase(configuration.GetNeo4jDatabaseName()));
        var invalid = false;
        for (var i = 0; i < queries.Length; i++)
        {
            var rows = await session.ExecuteWriteAsync(async tx =>
            {
                var cursor = await tx.RunAsync(queries[i]);
                return await cursor.ToListAsync(r => r.Values);
            });
            Console.WriteLine($"OK: migration {i + 1}/{queries.Length}");
            foreach (var row in rows)
            {
                Console.WriteLine(JsonSerializer.Serialize(row));
                invalid |= row.Keys.Any(k => k.StartsWith("invalid") || k is "cycle" or "missingPublicationReview");
            }
        }
        if (invalid)
            throw new InvalidOperationException("Migration đã chạy nhưng kiểm tra dữ liệu phát hiện lỗi; xem kết quả phía trên. Các bước thành công đã được lưu.");
        Console.WriteLine("PASS: migration và kiểm tra dữ liệu.");
    }

    public static async Task MigrateAttemptsAsync(IDriver driver, IConfiguration configuration)
    {
        await using var session = driver.AsyncSession(c => c.WithDatabase(configuration.GetNeo4jDatabaseName()));
        var cursor = await session.RunAsync("MATCH (a:Attempt) WHERE a.stateJson IS NOT NULL RETURN a.stateJson AS json");
        var states = await cursor.ToListAsync(r => JsonSerializer.Deserialize<AttemptState>(r["json"].As<string>())!);
        foreach (var state in states)
            await session.ExecuteWriteAsync(tx => AttemptSnapshots.WriteAsync(tx, state));
        Console.WriteLine($"OK: snapshot đồ thị cho {states.Count} lượt cũ.");
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

    public static IReadOnlyList<string> ReadVerificationStatements(string source)
    {
        const string marker = "// 7. KIỂM TRA CẤU TRÚC LUYỆN TẬP";
        var start = source.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0) throw new InvalidOperationException("Seed tổng hợp thiếu phần kiểm tra dữ liệu (phần 7).");
        var queries = SplitStatements(source[start..]).ToArray();
        if (queries.Length == 0) throw new InvalidOperationException("Phần kiểm tra dữ liệu không có truy vấn.");
        return queries;
    }

    public static async Task VerifyAsync(IDriver driver, string root, IConfiguration configuration)
    {
        await using var session = driver.AsyncSession(c => c.WithDatabase(configuration.GetNeo4jDatabaseName()));
        bool invalid = false;
        var source = await File.ReadAllTextAsync(Path.Combine(root, "migration", "seed-all.cypher"));
        foreach (var query in ReadVerificationStatements(source))
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
