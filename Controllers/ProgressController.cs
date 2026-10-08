using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Neo4j.Driver;
using Nosql_Neo4j.Models;
using Nosql_Neo4j.Repositories;
using Nosql_Neo4j.Services;
namespace Nosql_Neo4j.Controllers;

[Authorize, ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class ProgressController(IPracticeReportService reports, IShapeRepository shapes,
    IWebHostEnvironment environment, ILogger<ProgressController> logger) : Controller
{
    [HttpGet] public Task<IActionResult> History(PracticeReportFilter filter) => Report(filter, false);
    [HttpGet] public Task<IActionResult> Statistics(PracticeReportFilter filter) => Report(filter, true);
    private async Task<IActionResult> Report(PracticeReportFilter filter, bool statistics)
    {
        if (filter.Demo && !environment.IsDevelopment()) return NotFound();
        try
        {
            var topics = filter.Demo ? await shapes.GetDraftAsync() : await shapes.GetPublishedAsync();
            filter.TopicId = string.IsNullOrWhiteSpace(filter.TopicId) ? null : filter.TopicId;
            if (filter.TopicId is not null && !topics.Any(t => t.Id == filter.TopicId))
                ModelState.AddModelError("", "Chủ đề không hợp lệ.");
            try { filter.Bounds(); }
            catch (ArgumentException e) { ModelState.AddModelError("", e.Message); }
            if (!ModelState.IsValid)
            {
                Response.StatusCode = 400;
                return View(statistics ? "Statistics" : "History", new PracticeReportViewModel(filter, topics, environment.IsDevelopment()));
            }
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            return View(statistics ? "Statistics" : "History", new PracticeReportViewModel(filter, topics,
                environment.IsDevelopment(), statistics ? null : await reports.HistoryAsync(userId, filter),
                statistics ? await reports.StatisticsAsync(userId, filter) : null));
        }
        catch (Neo4jException e)
        {
            logger.LogError("Không đọc được lịch sử/thống kê; loại lỗi {Type}; request {RequestId}", e.GetType().Name, HttpContext.TraceIdentifier);
            Response.StatusCode = 503;
            return View("~/Views/Practice/Problem.cshtml", "Dịch vụ dữ liệu tạm thời không sẵn sàng. Hãy thử lại.");
        }
    }
}
