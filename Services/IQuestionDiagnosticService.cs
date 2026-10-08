using Nosql_Neo4j.Models.ViewModels;

namespace Nosql_Neo4j.Services;

// Chẩn đoán Development; không tạo lượt hoặc chấm điểm.
public interface IQuestionDiagnosticService
{
    Task<DraftQuestionsViewModel> GetDraftByTopicAsync(string topicId);
}
