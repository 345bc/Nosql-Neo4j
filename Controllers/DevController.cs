using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Neo4j.Driver;
using Nosql_Neo4j.Models.ViewModels;
using Nosql_Neo4j.Services;

namespace Nosql_Neo4j.Controllers;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public class DevController(IShapeDiagnosticService service, IWebHostEnvironment environment,
    ILogger<DevController> logger) : Controller
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!environment.IsDevelopment()) context.Result = NotFound();
        base.OnActionExecuting(context);
    }

    [HttpGet]
    public async Task<IActionResult> Shapes()
    {
        try { return View(new DraftShapesViewModel { Shapes = await service.GetDraftAsync() }); }
        catch (Neo4jException exception)
        {
            return DatabaseError(nameof(Shapes), new DraftShapesViewModel { ErrorMessage = DatabaseMessage }, exception);
        }
    }

    [HttpGet]
    public async Task<IActionResult> ShapeDetails(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound();
        try
        {
            var shape = await service.GetDraftByIdAsync(id);
            return shape is null ? NotFound() : View(new DraftShapeDetailsViewModel { Shape = shape });
        }
        catch (Neo4jException exception)
        {
            return DatabaseError(nameof(ShapeDetails), new DraftShapeDetailsViewModel { ErrorMessage = DatabaseMessage }, exception);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Graph()
    {
        try
        {
            return View(new DraftGraphViewModel { Graph = await service.GetDraftGraphAsync("HINH_VUONG", "TU_GIAC") });
        }
        catch (Neo4jException exception)
        {
            return DatabaseError(nameof(Graph), new DraftGraphViewModel { ErrorMessage = DatabaseMessage }, exception);
        }
    }

    private const string DatabaseMessage = "Không đọc được dữ liệu Neo4j. Kiểm tra instance và database nosql-neo4j.";
    private IActionResult DatabaseError(string view, object model, Neo4jException exception)
    {
        logger.LogError("Không đọc được dữ liệu. Loại lỗi: {ErrorType}; requestId: {RequestId}",
            exception.GetType().Name, HttpContext.TraceIdentifier);
        Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        return View(view, model);
    }
}
