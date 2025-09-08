using BrowserFile.Data;
using BrowserFile.Models.Entities;
using BrowserFile.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BrowserFile.Controllers
{
    public class FolderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FolderController> _logger;

        public FolderController(ApplicationDbContext context, ILogger<FolderController> logger)
        {
            _context = context;
            _logger = logger;
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

        [HttpPost]
        [Authorize]
        public IActionResult Create(FolderViewModel folderViewModel)
        {
            var icon = _context.Icons.Find(folderViewModel.FolderToCreate.IconId);
            if (icon == null)
            {
                ModelState.AddModelError("FolderToCreate.IconId", "Invalid icon selected.");
            }

            var folder = new Folder
            {
                Id = Guid.NewGuid().ToString(),
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                IconId = folderViewModel.FolderToCreate.IconId,
                Name = folderViewModel.FolderToCreate.Name,
                Description = folderViewModel.FolderToCreate.Description,
                CreatedAt = DateTime.UtcNow,
                Tag = folderViewModel.FolderToCreate.Tag
            };

            _context.Folders.Add(folder);
            _context.SaveChanges();

            _logger.LogInformation("Folder created: {FolderName} by User: {UserId}", folder.Name, folder.UserId);

            return RedirectToAction("Index");
        }
    }
}
