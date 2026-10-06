using Neo4j.Driver;
using Nosql_Neo4j.Models;
namespace Nosql_Neo4j.Repositories;

internal static class AttemptSnapshots
{
    // stateJson remains the authoritative immutable snapshot for grading and historical results.
    // Graph items support inspection and later shared reporting without reading live questions.
    public static async Task WriteAsync(IAsyncQueryRunner tx, AttemptState state)
    {
        var items = state.Questions.Select((snapshot, position) => new Dictionary<string, object?>
        {
            ["id"] = snapshot.Question.ItemId, ["position"] = position,
            ["questionId"] = snapshot.Question.QuestionId, ["versionId"] = snapshot.Question.VersionId,
            ["topicId"] = snapshot.Question.TopicId, ["prompt"] = snapshot.Question.Prompt,
            ["optionKeys"] = snapshot.Question.Options.Select(o => o.Key).ToArray(),
            ["optionTexts"] = snapshot.Question.Options.Select(o => o.Text).ToArray(),
            ["correctKey"] = snapshot.CorrectKey, ["explanation"] = snapshot.Explanation,
            ["selectedKey"] = state.Answers?.GetValueOrDefault(snapshot.Question.ItemId),
            ["isCorrect"] = state.Status == "SUBMITTED"
                ? state.Answers?.GetValueOrDefault(snapshot.Question.ItemId) == snapshot.CorrectKey : (bool?)null
        }).ToArray();
        await (await tx.RunAsync("""
            MATCH (a:Attempt {id: $id})
            SET a.topicIds = $topicIds
            WITH a
            UNWIND $items AS row
            MERGE (i:AttemptItem {id: row.id})
            ON CREATE SET i.position = row.position, i.questionId = row.questionId,
                i.versionId = row.versionId, i.topicId = row.topicId, i.prompt = row.prompt,
                i.optionKeys = row.optionKeys, i.optionTexts = row.optionTexts,
                i.correctKey = row.correctKey, i.explanation = row.explanation
            SET i.selectedKey = row.selectedKey, i.isCorrect = row.isCorrect
            MERGE (a)-[:HAS_ITEM]->(i)
            WITH i, row
            OPTIONAL MATCH (v:QuestionVersion {id: row.versionId})
            FOREACH (_ IN CASE WHEN v IS NULL THEN [] ELSE [1] END | MERGE (i)-[:OF_VERSION]->(v))
            """, new { id = state.Id, items,
            topicIds = state.Questions.Select(q => q.Question.TopicId).Distinct().ToArray() })).ConsumeAsync();
    }
}
