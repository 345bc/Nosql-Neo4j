using System.ComponentModel.DataAnnotations;
namespace Nosql_Neo4j.Models;

public sealed class PracticeReportFilter
{
    public string? TopicId { get; set; }
    [DataType(DataType.Date)] public DateOnly? From { get; set; }
    [DataType(DataType.Date)] public DateOnly? To { get; set; }
    public bool Demo { get; set; }
    [Range(1, 100000)] public int Page { get; set; } = 1;
    public (DateTimeOffset? Start, DateTimeOffset? End) Bounds()
    {
        if (From > To || To == DateOnly.MaxValue)
            throw new ArgumentException("Khoảng ngày không hợp lệ.");
        var offset = TimeSpan.FromHours(7);
        return (From is { } from ? new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), offset).ToUniversalTime() : null,
            To is { } to ? new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), offset).ToUniversalTime() : null);
    }
}
public sealed record PracticeHistoryRow(string Id, string? TopicId, DateTimeOffset SubmittedAt, int Score);
public sealed record PracticeHistoryPage(IReadOnlyList<PracticeHistoryRow> Rows, long Total, int Page, int PageSize);
public sealed record TopicAccuracy(string TopicId, long Correct, long Total)
{
    public decimal Percent => Total == 0 ? 0 : Math.Round(100m * Correct / Total, 1, MidpointRounding.AwayFromZero);
}
public sealed record PracticeStatistics(long Attempts, decimal? Average, IReadOnlyList<TopicAccuracy> Topics);
public sealed record PracticeReportViewModel(PracticeReportFilter Filter,
    IReadOnlyList<ShapeSummary> Topics, bool ShowDemo, PracticeHistoryPage? History = null,
    PracticeStatistics? Statistics = null);
