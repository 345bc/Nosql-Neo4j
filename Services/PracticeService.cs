using System.Security.Cryptography;
using Nosql_Neo4j.Models;
using Nosql_Neo4j.Repositories;
namespace Nosql_Neo4j.Services;

public sealed class PracticeService(IPracticeRepository repository, TimeProvider clock,
    IWebHostEnvironment environment) : IPracticeService
{
    public Task<string> CreateAsync(string userId, string? topicId)
        => CreateCoreAsync(userId, topicId, false);

    public Task<string> CreateDemoAsync(string userId, string? topicId)
    {
        if (!environment.IsDevelopment())
            throw new PracticeException("DEMO_DISABLED", "Chế độ thử chỉ có trong Development.");
        return CreateCoreAsync(userId, topicId, true);
    }

    private async Task<string> CreateCoreAsync(string userId, string? topicId, bool isDemo)
    {
        topicId = string.IsNullOrWhiteSpace(topicId) ? null : topicId.Trim();
        var snapshots = isDemo ? await repository.GetDraftCandidatesAsync(topicId)
            : await repository.GetCandidatesAsync(topicId);
        var candidates = snapshots
            .Where(ValidQuestion).GroupBy(q => q.Question.QuestionId)
            .Where(g => g.Count() == 1).Select(g => g.Single()).ToList();
        if (candidates.Count < 10)
            throw new PracticeException("INSUFFICIENT_QUESTIONS",
                $"Chủ đề hiện có {candidates.Count} câu hợp lệ {(isDemo ? "bản nháp" : "đã công bố")}; cần ít nhất 10 câu.");
        // Fisher-Yates: select without replacement.
        for (var i = candidates.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }
        var questions = candidates.Take(10).Select(q => q with
        {
            Question = q.Question with { ItemId = Guid.NewGuid().ToString("N") }
        }).ToArray();
        var now = clock.GetUtcNow();
        var attempt = new AttemptState(Guid.NewGuid().ToString("N"), userId, topicId,
            now, now.AddHours(24), "IN_PROGRESS", questions, IsDemo: isDemo);
        await repository.CreateAsync(attempt);
        return attempt.Id;
    }

    public async Task<PracticePaper?> GetPaperAsync(string userId, string id)
    {
        var state = await repository.ReadAsync(id, userId);
        if (state is null || (state.IsDemo && !environment.IsDevelopment())) return null;
        if (state.Status == "SUBMITTED") throw new PracticeException("SUBMITTED", "Lượt này đã nộp.");
        EnsureNotExpired(state);
        return new PracticePaper(state.Id, state.TopicId, state.ExpiresAt,
            state.Questions.Select(q => q.Question).ToArray(), state.IsDemo);
    }

    public async Task<PracticeResult?> GetResultAsync(string userId, string id)
    {
        var state = await repository.ReadAsync(id, userId);
        if (state is null || (state.IsDemo && !environment.IsDevelopment())) return null;
        if (state.Status != "SUBMITTED")
        {
            EnsureNotExpired(state);
            throw new PracticeException("IN_PROGRESS", "Lượt này chưa nộp.");
        }
        return ToResult(state);
    }

    public async Task<PracticeResult?> SubmitAsync(string userId, string id, PracticeSubmitInput input)
    {
        var state = await repository.UpdateLockedAsync(id, userId, current =>
        {
            if (current.IsDemo && !environment.IsDevelopment())
                throw new PracticeException("DEMO_DISABLED", "Chế độ thử chỉ có trong Development.");
            var answers = NormalizeAnswers(current, input);
            if (current.Status == "SUBMITTED")
            {
                if (current.Answers is null || answers.Any(a =>
                    !current.Answers.TryGetValue(a.Key, out var previous) || previous != a.Value))
                    throw new PracticeException("ATTEMPT_ALREADY_SUBMITTED",
                        "Bài đã nộp, không thể thay đổi đáp án. Bạn có thể mở kết quả đã lưu.");
                return current;
            }
            EnsureNotExpired(current);
            var score = current.Questions.Count(q => answers[q.Question.ItemId] == q.CorrectKey);
            return current with { Status = "SUBMITTED", Answers = answers,
                Score = score, SubmittedAt = clock.GetUtcNow() };
        });
        return state is null ? null : ToResult(state);
    }

    private void EnsureNotExpired(AttemptState state)
    {
        if (clock.GetUtcNow() >= state.ExpiresAt || state.Status == "EXPIRED")
            throw new PracticeException("ATTEMPT_EXPIRED", "Lượt làm bài đã hết hạn 24 giờ. Hãy tạo lượt mới.");
    }

    private static Dictionary<string, string?> NormalizeAnswers(AttemptState state, PracticeSubmitInput input)
    {
        var answers = state.Questions.ToDictionary(q => q.Question.ItemId, _ => (string?)null);
        var seen = new HashSet<string>();
        foreach (var item in input.Items)
        {
            if (string.IsNullOrWhiteSpace(item.ItemId) || !answers.ContainsKey(item.ItemId) || !seen.Add(item.ItemId))
                throw new PracticeException("VALIDATION_ERROR", "Danh sách câu trả lời có mã trùng hoặc ngoài đề.");
            var key = string.IsNullOrWhiteSpace(item.SelectedKey) ? null : item.SelectedKey;
            if (key is not null && key is not ("A" or "B" or "C" or "D"))
                throw new PracticeException("VALIDATION_ERROR", "Đáp án phải là A/B/C/D hoặc bỏ trống.");
            answers[item.ItemId] = key;
        }
        return answers;
    }

    private static bool ValidQuestion(QuestionSnapshot q)
        => !string.IsNullOrWhiteSpace(q.Question.QuestionId)
        && !string.IsNullOrWhiteSpace(q.Question.VersionId)
        && !string.IsNullOrWhiteSpace(q.Question.Prompt)
        && !string.IsNullOrWhiteSpace(q.Explanation)
        && q.Question.Options.Length == 4
        && q.Question.Options.Select(o => o.Key).OrderBy(k => k).SequenceEqual(new[] { "A", "B", "C", "D" })
        && q.Question.Options.All(o => !string.IsNullOrWhiteSpace(o.Text))
        && q.Question.Options.Select(o => o.Text.Trim()).Distinct().Count() == 4
        && q.CorrectKey is "A" or "B" or "C" or "D";

    private static PracticeResult ToResult(AttemptState state)
    {
        var items = state.Questions.Select(q =>
        {
            var selected = state.Answers![q.Question.ItemId];
            return new PracticeResultItem(q.Question, selected, q.CorrectKey,
                selected == q.CorrectKey, q.Explanation);
        }).ToArray();
        return new PracticeResult(state.Id, state.SubmittedAt!.Value, state.Score!.Value,
            items.Count(i => i.SelectedKey is not null && !i.IsCorrect),
            items.Count(i => i.SelectedKey is null), items, state.IsDemo);
    }
}
