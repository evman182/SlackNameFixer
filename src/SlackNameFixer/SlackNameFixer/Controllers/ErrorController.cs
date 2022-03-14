using Microsoft.AspNetCore.Mvc;

namespace SlackNameFixer.Controllers;

public class ErrorController : Controller
{
    [Route("/error")]
    public IActionResult HandleError() => Problem();
}