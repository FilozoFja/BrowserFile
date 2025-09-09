using AutoMapper;
using BrowserFile.Data;
using BrowserFile.Models.DTO;
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
        private readonly IMapper _mapper;

        public FolderController(ApplicationDbContext context, ILogger<FolderController> logger, IMapper mapper)
        {
            _context = context;
            _logger = logger;
            _mapper = mapper;
        }

        [HttpGet]
        [Authorize]
        public IActionResult Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var vm = new FolderViewModel
            {
                Folders = _context.Folders.Where(f => f.UserId == userId).ToList(),
                Icons = _context.Icons.ToList(),
                FolderToCreate = new FolderDTO()
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

            var folder = _mapper.Map<Folder>(folderViewModel.FolderToCreate);

            folder.Id = Guid.NewGuid().ToString();
            folder.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            folder.CreatedAt = DateTime.UtcNow;

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

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var folder = _context.Folders.FirstOrDefault(f => f.Id == id && f.UserId == userId);

            if(folder == null)
            {
                return NotFound();
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

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var originalFolder = _context.Folders.FirstOrDefault(x => x.UserId == userId && x.Id == id);

            if(originalFolder == null)
            {
                return NotFound();
            }

            var vm = new EditFolderViewModel
            {
                OriginalFolderId = originalFolder.Id,
                FolderToEdit = _mapper.Map<FolderDTO>(originalFolder),
                Icons = _context.Icons.ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [Authorize]
        public IActionResult Edit(EditFolderViewModel editFolderViewModel)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var originalFolder = _context.Folders.FirstOrDefault(f => f.Id == editFolderViewModel.OriginalFolderId && f.UserId == userId );

            if (originalFolder == null)
            {
                return NotFound();
            }

            originalFolder.Name = editFolderViewModel.FolderToEdit.Name;
            originalFolder.Description = editFolderViewModel.FolderToEdit.Description;
            originalFolder.Tag = editFolderViewModel.FolderToEdit.Tag;
            originalFolder.IconId = editFolderViewModel.FolderToEdit.IconId;

            _context.SaveChanges();

            _logger.LogInformation("Folder edited: {FolderName} by User: {UserId}", originalFolder.Name, originalFolder.UserId);

            return RedirectToAction("Index");
        }
    
    }
}
