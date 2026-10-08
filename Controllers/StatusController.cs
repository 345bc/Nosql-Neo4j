using Microsoft.AspNetCore.Mvc;
using Nosql_Neo4j.Models.ViewModels;

namespace Nosql_Neo4j.Controllers;

public sealed class StatusController : Controller
{
    [HttpGet("/Status")]
    public IActionResult Index(int code = 404)
    {
        var status = code is >= 400 and <= 599 ? code : 404;
        Response.StatusCode = status;
        var message = status == 404 ? "Không tìm thấy trang hoặc hình chưa được công bố." : "Không thể xử lý yêu cầu.";
        return View("~/Views/Shared/Status.cshtml", new StatusViewModel(status, message));
    }
}