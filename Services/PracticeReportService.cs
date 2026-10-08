using Nosql_Neo4j.Models;
using Nosql_Neo4j.Repositories;
namespace Nosql_Neo4j.Services;

public interface IPracticeReportService
{
    Task<PracticeHistoryPage> HistoryAsync(string userId, PracticeReportFilter filter);
    Task<PracticeStatistics> StatisticsAsync(string userId, PracticeReportFilter filter);
}
public sealed class PracticeReportService(PracticeReportRepository repository,
    IWebHostEnvironment environment) : IPracticeReportService
{
    private void Validate(PracticeReportFilter filter)
    {
        if (filter.Demo && !environment.IsDevelopment())
            throw new PracticeException("DEMO_DISABLED", "Chế độ thử chỉ có trong Development.");
        if (filter.Page is < 1 or > 100000) throw new ArgumentException("Trang không hợp lệ.");
        filter.Bounds();
    }
    public Task<PracticeHistoryPage> HistoryAsync(string userId, PracticeReportFilter filter)
    { Validate(filter); return repository.HistoryAsync(userId, filter); }
    public Task<PracticeStatistics> StatisticsAsync(string userId, PracticeReportFilter filter)
    { Validate(filter); return repository.StatisticsAsync(userId, filter); }
}
