using Nosql_Neo4j.Configuration;
using System.Text.Json;
using Neo4j.Driver;
using Nosql_Neo4j.Models;
namespace Nosql_Neo4j.Repositories;

public sealed class PracticeRepository(IDriver driver, IConfiguration configuration) : IPracticeRepository
{
    private readonly string _database = configuration.GetNeo4jDatabaseName();

    public Task<IReadOnlyList<QuestionSnapshot>> GetCandidatesAsync(string? topicId)
        => GetCandidatesByStatusAsync(topicId, "PUBLISHED");

    public Task<IReadOnlyList<QuestionSnapshot>> GetDraftCandidatesAsync(string? topicId)
        => GetCandidatesByStatusAsync(topicId, "DRAFT");

    private async Task<IReadOnlyList<QuestionSnapshot>> GetCandidatesByStatusAsync(string? topicId, string status)
    {
        await using var session = driver.AsyncSession(c => c.WithDatabase(_database));
        return await session.ExecuteReadAsync<IReadOnlyList<QuestionSnapshot>>(async tx =>
        {
            var cursor = await tx.RunAsync("""
                MATCH (q:Question)-[:CURRENT]->(v:QuestionVersion)
                WITH q, collect(v) AS versions
                WHERE size(versions) = 1
                WITH q, versions[0] AS v
                MATCH (v)-[:ABOUT]->(s:Shape)
                WITH q, v, collect(s) AS topics
                WHERE size(topics) = 1
                WITH q, v, topics[0] AS s
                WHERE v.status = $status AND s.status = $status
                  AND ($topicId IS NULL OR s.id = $topicId)
                RETURN q.id AS questionId, v.id AS versionId, s.id AS topicId,
                       coalesce(v.prompt, '') AS prompt,
                       coalesce(v.optionA, '') AS a, coalesce(v.optionB, '') AS b,
                       coalesce(v.optionC, '') AS c, coalesce(v.optionD, '') AS d,
                       coalesce(v.correctKey, '') AS correctKey,
                       coalesce(v.explanation, '') AS explanation
                """, new { topicId, status });
            var questions = new List<QuestionSnapshot>();
            while (await cursor.FetchAsync())
            {
                var r = cursor.Current;
                questions.Add(new QuestionSnapshot(new PracticeQuestion("", r["questionId"].As<string>(),
                    r["versionId"].As<string>(), r["topicId"].As<string>(), r["prompt"].As<string>(),
                    [new("A", r["a"].As<string>()), new("B", r["b"].As<string>()),
                     new("C", r["c"].As<string>()), new("D", r["d"].As<string>())]),
                    r["correctKey"].As<string>(), r["explanation"].As<string>()));
            }
            return questions;
        });
    }

    public async Task CreateAsync(AttemptState attempt)
    {
        await using var session = driver.AsyncSession(c => c.WithDatabase(_database));
        await session.ExecuteWriteAsync(async tx =>
        {
            var cursor = await tx.RunAsync("""
                MATCH (u:User {id: $userId, status: 'ACTIVE'})
                CREATE (a:Attempt {id: $id, stateJson: $stateJson, status: 'IN_PROGRESS',
                    startedAt: datetime($startedAt), expiresAt: datetime($expiresAt),
                    roleAtStart: u.role, isDemo: $isDemo, topicIds: $topicIds})
                CREATE (u)-[:STARTED]->(a)
                RETURN a.id AS id
                """, new { userId = attempt.UserId, id = attempt.Id,
                stateJson = JsonSerializer.Serialize(attempt), isDemo = attempt.IsDemo,
                topicIds = attempt.Questions.Select(q => q.Question.TopicId).Distinct().ToArray(),
                startedAt = attempt.StartedAt.ToString("O"), expiresAt = attempt.ExpiresAt.ToString("O") });
            if (!await cursor.FetchAsync())
                throw new PracticeException("UNAUTHENTICATED", "Tài khoản không còn hoạt động.");
            await cursor.ConsumeAsync();
            await AttemptSnapshots.WriteAsync(tx, attempt);
        });
    }

    public async Task<AttemptState?> ReadAsync(string id, string userId)
    {
        await using var session = driver.AsyncSession(c => c.WithDatabase(_database));
        return await session.ExecuteReadAsync<AttemptState?>(tx => ReadStateAsync(tx, id, userId));
    }

    public async Task<AttemptState?> UpdateLockedAsync(string id, string userId,
        Func<AttemptState, AttemptState> update)
    {
        await using var session = driver.AsyncSession(c => c.WithDatabase(_database));
        return await session.ExecuteWriteAsync<AttemptState?>(async tx =>
        {
            // Neo4j acquires a write lock before reading this dependent property.
            // Separate read below observes state after acquiring the node lock.
            var locked = await tx.RunAsync("""
                MATCH (:User {id: $userId})-[:STARTED]->(a:Attempt {id: $id})
                SET a.lockRevision = coalesce(a.lockRevision, 0) + 1
                RETURN a.id AS id
                """, new { userId, id });
            if (!await locked.FetchAsync()) return null;
            await locked.ConsumeAsync();
            var state = await ReadStateAsync(tx, id, userId);
            if (state is null) return null;
            var changed = update(state);
            await (await tx.RunAsync("""
                MATCH (:User {id: $userId})-[:STARTED]->(a:Attempt {id: $id})
                SET a.stateJson = $stateJson, a.status = $status, a.score = $score,
                    a.submittedAt = CASE WHEN $submittedAt IS NULL THEN null ELSE datetime($submittedAt) END
                """, new { userId, id, stateJson = JsonSerializer.Serialize(changed),
                status = changed.Status, score = changed.Score,
                submittedAt = changed.SubmittedAt?.ToString("O") })).ConsumeAsync();
            await AttemptSnapshots.WriteAsync(tx, changed);
            return changed;
        });
    }

    private static async Task<AttemptState?> ReadStateAsync(IAsyncQueryRunner tx, string id, string userId)
    {
        var cursor = await tx.RunAsync("""
            MATCH (:User {id: $userId})-[:STARTED]->(a:Attempt {id: $id})
            RETURN a.stateJson AS stateJson
            """, new { userId, id });
        if (!await cursor.FetchAsync()) return null;
        return JsonSerializer.Deserialize<AttemptState>(cursor.Current["stateJson"].As<string>())
            ?? throw new InvalidOperationException("Dữ liệu lượt làm bài không hợp lệ.");
    }
}
