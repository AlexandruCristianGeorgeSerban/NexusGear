using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using NexusGear.Models;

namespace NexusGear.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int? statusCode = null)
    {
        var effectiveStatusCode = statusCode ?? HttpContext.Response.StatusCode;
        if (effectiveStatusCode == 200) effectiveStatusCode = 404;

        var model = new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            StatusCode = effectiveStatusCode
        };

        return View(model);
    }
}
