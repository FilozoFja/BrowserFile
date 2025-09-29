using AutoMapper;
using BrowserFile.Interface;
using BrowserFile.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BrowserFile.Controllers
{
    public class FolderController : Controller
    {
        private readonly IFolderService _folderService;
        private readonly ILogger<FolderController> _logger;
        private readonly IMapper _mapper;
        private string CurrentUserId => User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        public FolderController(IFolderService folderService, ILogger<FolderController> logger, IMapper mapper)
        {
            _folderService = folderService;
            _logger = logger;
            _mapper = mapper;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var folders = await _folderService.GetUserFoldersAsync(CurrentUserId);
            var icons = await _folderService.GetIconsAsync();

            var vm = new FolderViewModel
            {
                Folders = folders,
                Icons = icons,
                FolderToCreate = new Models.DTO.FolderDTO()
            };
            return View(vm);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create(FolderViewModel folderViewModel)
        {
            if (folderViewModel.FolderToCreate == null)
            {
                TempData["Error"] = "Invalid folder data.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(folderViewModel.FolderToCreate.Name))
            {
                TempData["Error"] = "Folder name is required.";
                return RedirectToAction("Index");
            }

            var success = await _folderService.CreateFolderAsync(folderViewModel.FolderToCreate, CurrentUserId);

            if (success)
            {
                TempData["Success"] = "Folder created successfully.";
            }
            else
            {
                TempData["Error"] = "An error occurred while creating the folder. Please check if the icon is valid.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Delete([FromRoute] string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["Error"] = "Invalid folder ID.";
                return RedirectToAction("Index");
            }

            // Check if folder has files first
            var hasFiles = await _folderService.FolderHasFilesAsync(id, CurrentUserId);
            if (hasFiles)
            {
                TempData["Error"] = "Cannot delete a folder that contains files. Please remove the files first.";
                return RedirectToAction("Index");
            }

            var success = await _folderService.DeleteFolderAsync(id, CurrentUserId);

            if (success)
            {
                TempData["Success"] = "Folder deleted successfully.";
            }
            else
            {
                TempData["Error"] = "Folder not found or you do not have permission to delete it.";
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Edit([FromRoute] string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "Invalid folder ID.";
                return RedirectToAction("Index");
            }

            var folder = await _folderService.GetFolderByIdAsync(id, CurrentUserId);
            if (folder == null)
            {
                TempData["Error"] = "Folder not found or you do not have permission to edit it.";
                return RedirectToAction("Index");
            }

            var icons = await _folderService.GetIconsAsync();

            var vm = new EditFolderViewModel
            {
                OriginalFolderId = folder.Id,
                FolderToEdit = _mapper.Map<Models.DTO.FolderDTO>(folder),
                Icons = icons
            };

            return View(vm);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Edit(EditFolderViewModel editFolderViewModel)
        {
            if (editFolderViewModel.FolderToEdit == null || 
                string.IsNullOrWhiteSpace(editFolderViewModel.FolderToEdit.Name))
            {
                TempData["Error"] = "Invalid folder data.";
                return RedirectToAction("Index");
            }

            var success = await _folderService.UpdateFolderAsync(
                editFolderViewModel.OriginalFolderId, 
                editFolderViewModel.FolderToEdit, 
                CurrentUserId);

            if (success)
            {
                TempData["Success"] = "Folder updated successfully.";
            }
            else
            {
                TempData["Error"] = "Folder not found or you do not have permission to edit it.";
            }

            return RedirectToAction("Index");
        }
    }
}