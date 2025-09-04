using Microsoft.AspNetCore.Mvc;

namespace BrowserFile.Controllers
{
    public class FileController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
