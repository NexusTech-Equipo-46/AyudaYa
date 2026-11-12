using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AyudaYa.Web.Controllers;

public sealed class VoluntariadoController : Controller
{
    // Esta vista reproduce el prototipo con información ficticia en el navegador.
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Index() => View();
}
