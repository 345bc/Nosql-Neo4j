using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Neo4j.Driver;
using Nosql_Neo4j.Models;
using Nosql_Neo4j.Models.ViewModels;
using Nosql_Neo4j.Repositories;
using Nosql_Neo4j.Services;
namespace Nosql_Neo4j.Controllers;

[Authorize]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public class PracticeController(IPracticeService practice, IShapeRepository shapes,
    ILogger<PracticeController> logger, IWebHostEnvironment environment) : Controller
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        try { return View(await StartModelAsync()); }
        catch (Neo4jException e) { return DatabaseError(e); }
    }

    [HttpPost]
    public async Task<IActionResult> Create(string? topicId)
    {
        try
        {
            var topics = await shapes.GetPublishedAsync();
            if (!string.IsNullOrEmpty(topicId) && !topics.Any(s => s.Id == topicId))
                return View("Index", await StartModelAsync(topicId, "Chủ đề không tồn tại hoặc chưa công bố."));
            var id = await practice.CreateAsync(UserId, topicId);
            return RedirectToAction(nameof(Take), new { id });
        }
        catch (PracticeException e)
        {
            try
            {
                return View("Index", await StartModelAsync(topicId, e.Message));
            }
            catch (Neo4jException databaseException) { return DatabaseError(databaseException); }
        }
        catch (Neo4jException e) { return DatabaseError(e); }
    }

    [HttpPost]
    public async Task<IActionResult> Demo(string? topicId)
    {
        if (!environment.IsDevelopment()) return NotFound();
        try
        {
            var topics = await shapes.GetDraftAsync();
            if (!string.IsNullOrEmpty(topicId) && !topics.Any(s => s.Id == topicId))
                return View("Index", await StartModelAsync(topicId, "Chủ đề thử không tồn tại hoặc không phải bản nháp."));
            var id = await practice.CreateDemoAsync(UserId, topicId);
            return RedirectToAction(nameof(Take), new { id });
        }
        catch (PracticeException e)
        {
            try { return View("Index", await StartModelAsync(topicId, e.Message)); }
            catch (Neo4jException databaseException) { return DatabaseError(databaseException); }
        }
        catch (Neo4jException e) { return DatabaseError(e); }
    }

    private async Task<PracticeStartViewModel> StartModelAsync(string? topicId = null, string? error = null)
        => new()
        {
            TopicId = topicId, ErrorMessage = error,
            Topics = await shapes.GetPublishedAsync(), ShowDemo = environment.IsDevelopment(),
            DemoTopics = environment.IsDevelopment() ? await shapes.GetDraftAsync() : []
        };

    [HttpGet]
    public async Task<IActionResult> Take(string id)
    {
        try
        {
            var paper = await practice.GetPaperAsync(UserId, id);
            if (paper is null) return NotFound();
            return View(new PracticeTakeViewModel(paper, new PracticeSubmitInput
            { Items = paper.Questions.Select(q => new PracticeAnswerInput { ItemId = q.ItemId }).ToList() }));
        }
        catch (PracticeException e) when (e.Code == "SUBMITTED")
        { return RedirectToAction(nameof(Result), new { id }); }
        catch (PracticeException e) { return PracticeError(e); }
        catch (Neo4jException e) { return DatabaseError(e); }
    }

    [HttpPost]
    public async Task<IActionResult> Submit(string id, PracticeSubmitInput input)
    {
        try
        {
            if (ModelState.IsValid)
            {
                var result = await practice.SubmitAsync(UserId, id, input);
                return result is null ? NotFound() : RedirectToAction(nameof(Result), new { id });
            }
            return await RedisplayAsync(id, input);
        }
        catch (PracticeException e) when (e.Code == "VALIDATION_ERROR")
        {
            ModelState.AddModelError("", e.Message);
            try { return await RedisplayAsync(id, input); }
            catch (PracticeException other) { return PracticeError(other); }
            catch (Neo4jException databaseException) { return DatabaseError(databaseException); }
        }
        catch (PracticeException e) { return PracticeError(e); }
        catch (Neo4jException e) { return DatabaseError(e); }
    }

    [HttpGet]
    public async Task<IActionResult> Result(string id)
    {
        try
        {
            var result = await practice.GetResultAsync(UserId, id);
            return result is null ? NotFound() : View(result);
        }
        catch (PracticeException e) when (e.Code == "IN_PROGRESS")
        { return RedirectToAction(nameof(Take), new { id }); }
        catch (PracticeException e) { return PracticeError(e); }
        catch (Neo4jException e) { return DatabaseError(e); }
    }

    private async Task<IActionResult> RedisplayAsync(string id, PracticeSubmitInput input)
    {
        var paper = await practice.GetPaperAsync(UserId, id);
        if (paper is null) return NotFound();
        var safeInput = new PracticeSubmitInput
        {
            Items = paper.Questions.Select(q => new PracticeAnswerInput
            {
                ItemId = q.ItemId,
                SelectedKey = input.Items.FirstOrDefault(i => i.ItemId == q.ItemId)?.SelectedKey
            }).ToList()
        };
        // Remove binding values from tampered hidden fields; preserve validation errors.
        var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToArray();
        ModelState.Clear();
        foreach (var error in errors) ModelState.AddModelError("", error);
        return View("Take", new PracticeTakeViewModel(paper, safeInput));
    }

    private IActionResult PracticeError(PracticeException e)
    {
        if (e.Code == "DEMO_DISABLED") return NotFound();
        Response.StatusCode = 409;
        return View("Problem", e.Message);
    }
    private IActionResult DatabaseError(Neo4jException e)
    {
        logger.LogError("Luyện tập không truy cập được Neo4j; loại lỗi {Type}; request {RequestId}",
            e.GetType().Name, HttpContext.TraceIdentifier);
        Response.StatusCode = 503;
        return View("Problem", "Dịch vụ dữ liệu tạm thời không sẵn sàng. Bài của bạn chưa được báo lưu thành công; hãy thử lại.");
    }
}
