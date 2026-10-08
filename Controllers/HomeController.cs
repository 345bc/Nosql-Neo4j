using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Nosql_Neo4j.Models.ViewModels;
namespace Nosql_Neo4j.Controllers;

public class HomeController : Controller
{
    [HttpGet] public IActionResult Index() => View();

    // Exception handler có thể re-execute cả POST nên không giới hạn Error chỉ GET.
    [Route("/Error")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public IActionResult Error()
        => View("~/Views/Shared/Error.cshtml", new ErrorViewModel(Activity.Current?.Id ?? HttpContext.TraceIdentifier));
}
