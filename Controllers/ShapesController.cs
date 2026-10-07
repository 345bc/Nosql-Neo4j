using Microsoft.AspNetCore.Mvc;
using Nosql_Neo4j.Contracts;
using Nosql_Neo4j.Models.ViewModels;

namespace Nosql_Neo4j.Controllers;

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ShapesController(IShapeService service, ILogger<ShapesController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? q, int page = 1, CancellationToken ct = default)
    {
        var model = new ShapeIndexViewModel(q);
        if (!ModelState.IsValid)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return View(model);
        }

        try { model = model with { Result = await service.ListAsync(q, page, ct) }; }
        catch (DomainValidationException ex) { Validation(ex); }
        catch (DatabaseUnavailableException ex) { model = model with { ErrorMessage = Unavailable(ex) }; }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(string? id, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound();
        var model = new ShapeDetailsViewModel();
        try
        {
            var detail = await service.GetDetailAsync(id, ct);
            if (detail is null) return NotFound();
            model = model with { Detail = detail };
        }
        catch (DomainValidationException ex) { Validation(ex); }
        catch (DatabaseUnavailableException ex) { model = model with { ErrorMessage = Unavailable(ex) }; }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Graph(string? fromId, string? toId, CancellationToken ct = default)
    {
        GraphVm? graph = null;
        string? errorMessage = null;
        try { graph = await service.GetGraphAsync(fromId, toId, ct); }
        catch (DomainValidationException ex)
        {
            Validation(ex);
            // Keep the selectors available when the chosen pair is invalid.
            try { graph = await service.GetGraphAsync(null, null, ct); }
            catch (DatabaseUnavailableException db) { errorMessage = Unavailable(db); }
            catch (DomainValidationException) { /* Invalid stored graph: show the original validation error. */ }
        }
        catch (DatabaseUnavailableException ex) { errorMessage = Unavailable(ex); }
        return View(ShapeGraphViewModel.Create(graph, fromId, toId, errorMessage));
    }

    private void Validation(DomainValidationException ex)
    {
        Response.StatusCode = StatusCodes.Status400BadRequest;
        foreach (var field in ex.Errors)
            foreach (var error in field.Value) ModelState.AddModelError(field.Key, error);
    }

    private string Unavailable(DatabaseUnavailableException ex)
    {
        // Driver messages may contain connection details; do not expose them in HTML or logs.
        logger.LogWarning("Knowledge database unavailable: {ErrorType}", ex.InnerException?.GetType().Name ?? ex.GetType().Name);
        Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        return "Chưa thể tải kiến thức từ Neo4j. Vui lòng thử lại hoặc kiểm tra cấu hình kết nối.";
    }
}