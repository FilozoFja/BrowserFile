using Microsoft.AspNetCore.Mvc;

namespace BrowserFile.Controllers
{
    public class FileController : Controller
    {
        [HttpGet]
        public IActionResult File()
        {
            return View();
        }
    }
}
