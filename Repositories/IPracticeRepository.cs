using Nosql_Neo4j.Models;
namespace Nosql_Neo4j.Repositories;

public interface IPracticeRepository
{
    Task<IReadOnlyList<QuestionSnapshot>> GetCandidatesAsync(string? topicId);
    Task<IReadOnlyList<QuestionSnapshot>> GetDraftCandidatesAsync(string? topicId);
    Task CreateAsync(AttemptState attempt);
    Task<AttemptState?> ReadAsync(string id, string userId);
    Task<AttemptState?> UpdateLockedAsync(string id, string userId,
        Func<AttemptState, AttemptState> update);
}
