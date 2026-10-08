using Nosql_Neo4j.Models;
namespace Nosql_Neo4j.Services;

public interface IPracticeService
{
    Task<string> CreateAsync(string userId, string? topicId);
    Task<string> CreateDemoAsync(string userId, string? topicId);
    Task<PracticePaper?> GetPaperAsync(string userId, string id);
    Task<PracticeResult?> GetResultAsync(string userId, string id);
    Task<PracticeResult?> SubmitAsync(string userId, string id, PracticeSubmitInput input);
}
