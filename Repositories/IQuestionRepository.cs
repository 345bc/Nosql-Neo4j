using Nosql_Neo4j.Models;

namespace Nosql_Neo4j.Repositories
{
    public interface IQuestionRepository
    {
        Task<IReadOnlyList<Question>> GetDraftByTopicAsync(
        string topicId);
    }
}
