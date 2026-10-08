using Nosql_Neo4j.Models;
using Nosql_Neo4j.Models.ViewModels;
using Nosql_Neo4j.Repositories;

namespace Nosql_Neo4j.Services;

public sealed class QuestionDiagnosticService(IQuestionRepository repository,
    IShapeRepository shapeRepository) : IQuestionDiagnosticService
{
    public async Task<DraftQuestionsViewModel> GetDraftByTopicAsync(string topicId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topicId);
        var topics = await shapeRepository.GetDraftAsync();
        if (!topics.Any(t => t.Id == topicId))
            return new DraftQuestionsViewModel
            {
                TopicId = topicId, Topics = topics,
                ValidationErrors = ["Chủ đề không tồn tại hoặc không có trạng thái DRAFT."]
            };

        var questions = await repository.GetDraftByTopicAsync(topicId);
        return new DraftQuestionsViewModel
        {
            TopicId = topicId, Topics = topics, Questions = questions,
            ValidationErrors = Validate(questions, topicId)
        };
    }

    private static IReadOnlyList<string> Validate(IReadOnlyList<Question> questions, string topicId)
    {
        var errors = new List<string>();
        foreach (var duplicate in questions.GroupBy(q => q.QuestionId).Where(g => g.Count() > 1))
            errors.Add($"Mã câu {duplicate.Key} xuất hiện nhiều lần; kiểm tra CURRENT/ABOUT.");
        foreach (var duplicate in questions.GroupBy(q => q.VersionId).Where(g => g.Count() > 1))
            errors.Add($"Mã phiên bản {duplicate.Key} xuất hiện nhiều lần.");
        foreach (var q in questions)
        {
            var label = string.IsNullOrWhiteSpace(q.QuestionId) ? "Câu chưa có mã" : q.QuestionId;
            if (string.IsNullOrWhiteSpace(q.QuestionId) || string.IsNullOrWhiteSpace(q.VersionId))
                errors.Add($"{label}: thiếu mã câu hoặc mã phiên bản.");
            if (q.TopicId != topicId) errors.Add($"{label}: không thuộc chủ đề đang chọn.");
            if (string.IsNullOrWhiteSpace(q.Prompt)) errors.Add($"{label}: thiếu nội dung câu hỏi.");
            if (q.Options.Count != 4 || !q.Options.Select(o => o.Key).OrderBy(k => k)
                .SequenceEqual(new[] { "A", "B", "C", "D" }))
                errors.Add($"{label}: cần đúng bốn khóa A/B/C/D, không trùng.");
            if (q.Options.Any(o => string.IsNullOrWhiteSpace(o.Text)))
                errors.Add($"{label}: có lựa chọn bị trống.");
            if (q.Options.Select(o => o.Text.Trim()).Distinct(StringComparer.Ordinal).Count() != q.Options.Count)
                errors.Add($"{label}: nội dung lựa chọn trùng sau khi bỏ khoảng trắng đầu/cuối.");
        }
        return errors;
    }
}
