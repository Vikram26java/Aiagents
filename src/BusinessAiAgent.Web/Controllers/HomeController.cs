using System.Diagnostics;
using BusinessAiAgent.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace BusinessAiAgent.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole("Admin")) return Redirect("/Admin");
            return Redirect("/Customer");
        }
        return Redirect("/Account/Login");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
