namespace Nosql_Neo4j.Models;

public sealed record Question(
    string QuestionId,
    string VersionId,
    string TopicId,
    string Prompt,
    IReadOnlyList<QuestionOption> Options);