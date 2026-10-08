namespace Nosql_Neo4j.Models.ViewModels;

public sealed class PracticeStartViewModel
{
    public string? TopicId { get; set; }
    public IReadOnlyList<ShapeSummary> Topics { get; set; } = [];
    public IReadOnlyList<ShapeSummary> DemoTopics { get; set; } = [];
    public bool ShowDemo { get; set; }
    public string? ErrorMessage { get; set; }
}
public sealed record PracticeTakeViewModel(PracticePaper Paper, PracticeSubmitInput Input);
