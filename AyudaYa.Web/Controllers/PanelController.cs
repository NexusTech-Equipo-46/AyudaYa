using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AyudaYa.Web.Controllers;

public sealed class PanelController : Controller
{
    // El panel reproduce el prototipo con datos ficticios en el navegador.
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Index() => View();
}
