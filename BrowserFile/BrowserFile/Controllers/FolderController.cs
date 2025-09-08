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
            var vm = new FolderViewModel
            {
                Folders = _context.Folders.ToList(),
                Icons = _context.Icons.ToList(),
                FolderToCreate = new Folder()
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
        [HttpPost]
        [Authorize]
        public IActionResult Delete([FromRoute]string id) 
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }
            var folder = _context.Folders.Find(id);
            if(folder == null)
            {
                return NotFound();
            }
            if(folder.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                return Forbid();
            }
            _context.Folders.Remove(folder);
            _context.SaveChanges();

            _logger.LogCritical("Folder deleted: {FolderName} by User: {UserId}", folder.Name, folder.UserId);

            return RedirectToAction("Index");
        }

        [HttpGet]
        [Authorize]
        public IActionResult Edit([FromRoute]string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var originalFolder = _context.Folders.Find(id);

            var vm = new EditFolderViewModel
            {
                OriginalFolder = originalFolder,
                FolderToCreate = new Folder
                {
                    Name = originalFolder.Name,
                    Description = originalFolder.Description,
                    Tag = originalFolder.Tag,
                    IconId = originalFolder.IconId
                },
                Icons = _context.Icons.ToList()
            };

            if(vm.OriginalFolder == null)
            {
                return NotFound();
            }

            if (User.FindFirstValue(ClaimTypes.NameIdentifier) != vm.OriginalFolder.UserId)
            {
                _logger.LogCritical("Unauthorized access attempt to edit folder {FolderId} by user {UserId}", id, User.FindFirstValue(ClaimTypes.NameIdentifier));
                return Forbid();
            }

            return View(vm);
        }

        [HttpPost]
        [Authorize]
        public IActionResult Edit(EditFolderViewModel editFolderViewModel)
        {
            var originalFolder = _context.Folders.Find(editFolderViewModel.OriginalFolder.Id);
            if (originalFolder == null)
            {
                return NotFound();
            }

            if (User.FindFirstValue(ClaimTypes.NameIdentifier) != originalFolder.UserId)
            {
                _logger.LogCritical("Unauthorized access attempt to edit folder {FolderId} by user {UserId}", editFolderViewModel.OriginalFolder.Id, User.FindFirstValue(ClaimTypes.NameIdentifier));
                return Forbid();
            }

            originalFolder.Name = editFolderViewModel.FolderToCreate.Name;
            originalFolder.Description = editFolderViewModel.FolderToCreate.Description;
            originalFolder.Tag = editFolderViewModel.FolderToCreate.Tag;
            originalFolder.IconId = editFolderViewModel.FolderToCreate.IconId;

            _context.Folders.Update(originalFolder);
            _context.SaveChanges();

            _logger.LogInformation("Folder edited: {FolderName} by User: {UserId}", originalFolder.Name, originalFolder.UserId);

            return RedirectToAction("Index");
        }
    
    }
}
