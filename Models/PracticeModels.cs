using System.ComponentModel.DataAnnotations;

namespace Nosql_Neo4j.Models;

public sealed record PracticeQuestion(string ItemId, string QuestionId, string VersionId,
    string TopicId, string Prompt, QuestionOption[] Options);
public sealed record PracticePaper(string Id, string? TopicId, DateTimeOffset ExpiresAt,
    PracticeQuestion[] Questions, bool IsDemo = false);
public sealed record PracticeResultItem(PracticeQuestion Question, string? SelectedKey,
    string CorrectKey, bool IsCorrect, string Explanation);
public sealed record PracticeResult(string Id, DateTimeOffset SubmittedAt, int Score,
    int IncorrectCount, int UnansweredCount, PracticeResultItem[] Items, bool IsDemo = false);

// Chỉ dùng trong repository/service. Không truyền snapshot này vào View.
public sealed record QuestionSnapshot(PracticeQuestion Question, string CorrectKey, string Explanation);
public sealed record AttemptState(string Id, string UserId, string? TopicId,
    DateTimeOffset StartedAt, DateTimeOffset ExpiresAt, string Status,
    QuestionSnapshot[] Questions, Dictionary<string, string?>? Answers = null,
    DateTimeOffset? SubmittedAt = null, int? Score = null, bool IsDemo = false);

public sealed class PracticeException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public sealed class PracticeAnswerInput
{
    [Required] public string ItemId { get; set; } = "";
    [RegularExpression("^[ABCD]$", ErrorMessage = "Lựa chọn phải là A/B/C/D.")]
    public string? SelectedKey { get; set; }
}

public sealed class PracticeSubmitInput
{
    public List<PracticeAnswerInput> Items { get; set; } = [];
}
