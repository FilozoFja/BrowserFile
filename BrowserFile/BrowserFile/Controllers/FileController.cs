using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BrowserFile.Controllers
{
    public class FileController : Controller
    {
        [HttpGet]
        [Authorize]
        public IActionResult File()
        {
            return View();
        }
    }
}
