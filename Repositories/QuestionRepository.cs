using Nosql_Neo4j.Configuration;
using Neo4j.Driver;
using Nosql_Neo4j.Models;

namespace Nosql_Neo4j.Repositories
{
    public sealed class QuestionRepository(IDriver driver, IConfiguration configuration):IQuestionRepository
    {
        private readonly string _database = configuration.GetNeo4jDatabaseName();

        public async Task<IReadOnlyList<Question>>GetDraftByTopicAsync(string topicId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(topicId);

            await using var session = driver.AsyncSession(
           config => config.WithDatabase(_database));

            return await session.ExecuteReadAsync(async transaction =>
            {
                var cursor = await transaction.RunAsync("""
                MATCH (q:Question)-[:CURRENT]->(v:QuestionVersion)
                      -[:ABOUT]->(s:Shape)
                WHERE s.id = $topicId AND v.status = $status
                RETURN q.id AS questionId,
                       v.id AS versionId,
                       s.id AS topicId,
                       coalesce(v.prompt, '') AS prompt,
                       coalesce(v.optionA, '') AS optionA,
                       coalesce(v.optionB, '') AS optionB,
                       coalesce(v.optionC, '') AS optionC,
                       coalesce(v.optionD, '') AS optionD
                ORDER BY questionId
                """,
                new { topicId, status = "DRAFT" });

                var questions = new List<Question>();

                while (await cursor.FetchAsync())
                {
                    var record = cursor.Current;

                    questions.Add(new Question(
                        record["questionId"].As<string>(),
                        record["versionId"].As<string>(),
                        record["topicId"].As<string>(),
                        record["prompt"].As<string>(),
                        new QuestionOption[]
                        {
                        new("A", record["optionA"].As<string>()),
                        new("B", record["optionB"].As<string>()),
                        new("C", record["optionC"].As<string>()),
                        new("D", record["optionD"].As<string>())
                        }));
                }

                return (IReadOnlyList<Question>)questions;

            });
        }
    }
}
