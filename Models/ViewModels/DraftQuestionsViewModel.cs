namespace Nosql_Neo4j.Models.ViewModels;

public sealed class DraftQuestionsViewModel
{
    public string TopicId { get; init; } = "HINH_VUONG";
    public IReadOnlyList<ShapeSummary> Topics { get; init; } = [];
    public IReadOnlyList<Question> Questions { get; init; } = [];
    public IReadOnlyList<string> ValidationErrors { get; init; } = [];
    public string? ErrorMessage { get; init; }
}
