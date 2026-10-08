using Nosql_Neo4j.Configuration;
using System.Text.Json;
using Neo4j.Driver;
using Nosql_Neo4j.Models;
namespace Nosql_Neo4j.Repositories;

public sealed class PracticeReportRepository(IDriver driver, IConfiguration configuration)
{
    private readonly string _database = configuration.GetNeo4jDatabaseName();

    private const string Match = """
        MATCH (:User {id: $userId})-[:STARTED]->(a:Attempt)
        WHERE a.status = 'SUBMITTED' AND coalesce(a.isDemo, false) = $demo
          AND ($start IS NULL OR a.submittedAt >= datetime($start))
          AND ($end IS NULL OR a.submittedAt < datetime($end))
        """;

    private static Dictionary<string, object?> Parameters(string userId, PracticeReportFilter filter)
    {
        var (start, end) = filter.Bounds();
        return new() { ["userId"] = userId, ["demo"] = filter.Demo,
            ["start"] = start?.ToString("O"), ["end"] = end?.ToString("O"), ["topicId"] = filter.TopicId };
    }

    public async Task<PracticeHistoryPage> HistoryAsync(string userId, PracticeReportFilter filter)
    {
        const int size = 10;
        var parameters = Parameters(userId, filter);
        parameters["skip"] = ((long)filter.Page - 1) * size;
        parameters["limit"] = size;
        // topicIds is captured at creation; old attempts have topicId in stateJson.
        const string topic = " AND ($topicId IS NULL OR $topicId IN coalesce(a.topicIds, [])) ";
        await using var session = driver.AsyncSession(c => c.WithDatabase(_database));
        return await session.ExecuteReadAsync(async tx =>
        {
            var count = await tx.RunAsync(Match + topic + " RETURN count(a) AS total", parameters);
            await count.FetchAsync();
            var total = count.Current["total"].As<long>();
            var cursor = await tx.RunAsync(Match + topic +
                " RETURN a.stateJson AS stateJson ORDER BY a.submittedAt DESC, a.id DESC SKIP $skip LIMIT $limit", parameters);
            var rows = new List<PracticeHistoryRow>();
            while (await cursor.FetchAsync())
            {
                var state = JsonSerializer.Deserialize<AttemptState>(cursor.Current["stateJson"].As<string>())!;
                rows.Add(new(state.Id, state.TopicId, state.SubmittedAt!.Value, state.Score!.Value));
            }
            return new PracticeHistoryPage(rows, total, filter.Page, size);
        });
    }

    public async Task<PracticeStatistics> StatisticsAsync(string userId, PracticeReportFilter filter)
    {
        await using var session = driver.AsyncSession(c => c.WithDatabase(_database));
        return await session.ExecuteReadAsync(async tx =>
        {
            var cursor = await tx.RunAsync(Match +
                " AND ($topicId IS NULL OR $topicId IN coalesce(a.topicIds, [])) RETURN a.stateJson AS stateJson",
                Parameters(userId, filter));
            long attempts = 0, sum = 0;
            var topics = new Dictionary<string, (long Correct, long Total)>();
            while (await cursor.FetchAsync())
            {
                var state = JsonSerializer.Deserialize<AttemptState>(cursor.Current["stateJson"].As<string>())!;
                attempts++; sum += state.Score!.Value;
                foreach (var item in state.Questions.Where(q => filter.TopicId is null || q.Question.TopicId == filter.TopicId))
                {
                    var old = topics.GetValueOrDefault(item.Question.TopicId);
                    var correct = state.Answers?.GetValueOrDefault(item.Question.ItemId) == item.CorrectKey;
                    topics[item.Question.TopicId] = (old.Correct + (correct ? 1 : 0), old.Total + 1);
                }
            }
            return new PracticeStatistics(attempts, attempts == 0 ? null : Math.Round((decimal)sum / attempts, 2,
                MidpointRounding.AwayFromZero), topics.OrderBy(t => t.Key)
                .Select(t => new TopicAccuracy(t.Key, t.Value.Correct, t.Value.Total)).ToArray());
        });
    }
}
