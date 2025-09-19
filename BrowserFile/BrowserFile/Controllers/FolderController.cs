using AutoMapper;
using BrowserFile.Data;
using BrowserFile.Models.DTO;
using BrowserFile.Models.Entities;
using BrowserFile.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace BrowserFile.Controllers
{
    public class FolderController : Controller
    {
        private const string ICONS_CACHE_KEY = "icons";
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FolderController> _logger;
        private readonly IMapper _mapper;
        private readonly IMemoryCache _cache;
        private string CurrentUserId => User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;


        public FolderController(ApplicationDbContext context, ILogger<FolderController> logger, IMapper mapper, IMemoryCache cache)
        {
            _context = context;
            _logger = logger;
            _mapper = mapper;
            _cache = cache;
        }

        private List<Icon> GetIcons()
        {
            if (!_cache.TryGetValue(ICONS_CACHE_KEY, out List<Icon> icons))
            {
                icons = _context.Icons.ToList();
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromHours(1));
                _cache.Set(ICONS_CACHE_KEY, icons, cacheEntryOptions);
            }
            return icons;
        }

        [HttpGet]
        [Authorize]
        public IActionResult Index()
        {
            var vm = new FolderViewModel
            {
                Folders = _context.Folders.Where(f => f.UserId == CurrentUserId).ToList(),
                Icons = GetIcons(),
                FolderToCreate = new FolderDTO()
            };
            return View(vm);
        }

        [HttpPost]
        [Authorize]
        public IActionResult Create(FolderViewModel folderViewModel)
        {
            if (folderViewModel.FolderToCreate == null)
            {
                TempData["Error"] = "Invalid folder data";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(folderViewModel.FolderToCreate.Name))
            {
                TempData["Error"] = "Folder name is required";
                return RedirectToAction("Index");
            }

            var selectedIcon = GetIcons().FirstOrDefault(i => i.Id == folderViewModel.FolderToCreate.IconId);
            if (selectedIcon == null)
            {
                TempData["Error"] = "Invalid icon selected";
                return RedirectToAction("Index");
            }

            var folder = _mapper.Map<Folder>(folderViewModel.FolderToCreate);

            folder.Id = Guid.NewGuid().ToString();
            folder.UserId = CurrentUserId;
            folder.CreatedAt = DateTime.UtcNow;
            try
            {
                _context.Folders.Add(folder);
                _context.SaveChanges();
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error creating folder: {FolderName} by User: {UserId}", folder.Name, CurrentUserId);
                TempData["Error"] = "An error occurred while creating the folder.";
                return RedirectToAction("Index");
            }

            _logger.LogInformation("Folder created: {FolderName} by User: {UserId}", folder.Name, CurrentUserId);

            TempData["Success"] = "Folder created successfully";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize]
        public IActionResult Delete([FromRoute]string id) 
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["Error"] = "Invalid folder ID";
                return RedirectToAction("Index");
            }

            var folder = _context.Folders
                        .Include(f => f.StoredFiles)   
                        .FirstOrDefault(f => f.Id == id && f.UserId == CurrentUserId);

            if (folder == null)
            {
                TempData["Error"] = "Folder not found or you do not have permission to delete it";
                return RedirectToAction("Index");
            }

            if(folder.StoredFiles.Any())
            {
                TempData["Error"] = "Cannot delete a folder that contains files. Please remove the files first.";
                return RedirectToAction("Index");
            }
            

            try
            {
                _context.Folders.Remove(folder);
                _context.SaveChanges();
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error deleting folder: {FolderName} by User: {UserId}", folder.Name, CurrentUserId);
                TempData["Error"] = "An error occurred while deleting the folder.";
                return RedirectToAction("Index");
            }

            _logger.LogWarning("Folder deleted: {FolderName} by User: {UserId}", folder.Name, CurrentUserId);

            TempData["Success"] = "Folder deleted successfully";
            return RedirectToAction("Index");
        }

        [HttpGet]
        [Authorize]
        public IActionResult Edit([FromRoute]string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "Invalid folder ID";
                return RedirectToAction("Index");
            }

            var originalFolder = _context.Folders.FirstOrDefault(x => x.UserId == CurrentUserId && x.Id == id);

            if(originalFolder == null)
            {
                TempData["Error"] = "Folder not found or you do not have permission to edit it";
                return RedirectToAction("Index");
            }

            var vm = new EditFolderViewModel
            {
                OriginalFolderId = originalFolder.Id,
                FolderToEdit = _mapper.Map<FolderDTO>(originalFolder),
                Icons = GetIcons()
            };

            return View(vm);
        }

        [HttpPost]
        [Authorize]
        public IActionResult Edit(EditFolderViewModel editFolderViewModel)
        {
            var originalFolder = _context.Folders.FirstOrDefault(f => f.Id == editFolderViewModel.OriginalFolderId && f.UserId == CurrentUserId);

            if (originalFolder == null)
            {
                TempData["Error"] = "Folder not found or you do not have permission to edit it";
                return RedirectToAction("Index");
            }

            originalFolder.Name = editFolderViewModel.FolderToEdit.Name;
            originalFolder.Description = editFolderViewModel.FolderToEdit.Description;
            originalFolder.Tag = editFolderViewModel.FolderToEdit.Tag;
            originalFolder.IconId = editFolderViewModel.FolderToEdit.IconId;
            try
            {
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing folder: {FolderName} by User: {UserId}", originalFolder.Name, CurrentUserId);
                TempData["Error"] = "An error occurred while editing the folder.";
                return RedirectToAction("Index");
            }
            _logger.LogInformation("Folder edited: {FolderName} by User: {UserId}", originalFolder.Name, CurrentUserId);

            TempData["Success"] = "Folder edited successfully";
            return RedirectToAction("Index");
        }
    
    }
}
