using BrowserFile.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BrowserFile.Controllers
{
    public class FolderController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FolderController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize]
        public IActionResult Index()
        {
            var vm = new Models.ViewModels.FolderViewModel
            {
                Folders = _context.Folders.ToList(),
                Icons = _context.Icons.ToList(),
                FolderToCreate = new Models.Entities.Folder()
            };
            return View(vm);
        }
    }
}
