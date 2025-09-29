using BrowserFile.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using BrowserFile.Models.ViewModels;
using BrowserFile.Models.Entities;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using BrowserFile.Interface;
using BrowserFile.Models.DTO;

namespace BrowserFile.Controllers
{
    public class FileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private string CurrentUserId => User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        private readonly IMapper _mapper;
        private readonly ILogger<FileController> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly IFileService _fileService;

        public FileController(ApplicationDbContext context, IConfiguration configuration, 
                                IMapper mapper, ILogger<FileController> logger,  
                                IWebHostEnvironment environment, IFileService fileService)
        {
            _context = context;
            _configuration = configuration;
            _mapper = mapper;
            _logger = logger;
            _environment = environment;
            _fileService = fileService;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Index([FromRoute] string id)
        {
            var files = await _fileService.GetFilesByFolderAsync(id, CurrentUserId);
            var folders = await _context.Folders.Where(f => f.UserId == CurrentUserId).ToListAsync();
            var currentFolder = folders.FirstOrDefault(f => f.Id == id);

            if (currentFolder == null || string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "Folder not found.";
                return RedirectToAction("Index", "Folder");
            }

            var vm = new FileViewModel
            {
                Files = files,
                CurrentFolderId = id,
                FolderName = currentFolder?.Name ?? "Root",
                FolderShortModel = _mapper.Map<List<FolderShortModelDTO>>(folders)
            };
            return View(vm);
        }

        [HttpGet("download/{id}")]
        [Authorize]
        public async Task<IActionResult> DownloadFile(string id)
        {
            var file = await _fileService.GetFileAsync(id, CurrentUserId);
            if (file == null)
            {
                TempData["Error"] = "File not found.";
                return RedirectToAction("Index", "Folder");
            }

            var fullFilePath = _fileService.GetSafeFilePath(file.FilePath);
            if (fullFilePath == null)
            {
                TempData["Error"] = "File not found on server.";
                return RedirectToAction("Index", "File");
            }

            try
            {
                var stream = new FileStream(fullFilePath, FileMode.Open, FileAccess.Read);
                var contentType = _fileService.GetContentType(file.FileExtension) ?? "application/octet-stream";

                _logger.LogInformation("File downloaded: {FileId} by user {UserId}", id, CurrentUserId);
                return File(stream, contentType, file.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file {FileId}", id);
                TempData["Error"] = "Error occurred while downloading the file.";
                return RedirectToAction("Index", "File");
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Preview(string id)
        {
            var file = await _fileService.GetFileAsync(id, CurrentUserId);
            if (file == null)
            {
                TempData["Error"] = "File not found.";
                return RedirectToAction("Index", "Folder");
            }

            var previewableExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".gif", ".txt" };
            if (!previewableExtensions.Contains(file.FileExtension?.ToLower()))
            {
                TempData["Error"] = "This file type cannot be previewed.";
                return RedirectToAction("Index", "Folder");
            }

            var fullFilePath = Path.Combine(Directory.GetCurrentDirectory(), file.FilePath);
            try
            {
                if (!System.IO.File.Exists(fullFilePath))
                {
                    TempData["Error"] = "File not found on server.";
                    return RedirectToAction("Index", "Folder");
                }
    
                var contentType = _fileService.GetContentType(file.FileExtension) ?? "application/octet-stream";
                return PhysicalFile(fullFilePath, contentType); 
            }
            catch (IOException)
            {
                TempData["Error"] = "File is currently in use. Please try again later.";
                return RedirectToAction("Index", "Folder");
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UploadFile(FileViewModel fileViewModel, IFormFile file)
        {
            if (!await _fileService.VerifyFolderPermissionAsync(fileViewModel.CurrentFolderId, CurrentUserId))
            {
                TempData["Error"] = "Folder not found or you don't have permission to upload files to this folder.";
                return RedirectToAction("Index", "Folder");
            }
            
            if (file?.FileName == null || string.IsNullOrWhiteSpace(file.FileName))
            {
                TempData["Error"] = "Please select a file to upload.";
                return RedirectToAction("Index", new { id = fileViewModel.CurrentFolderId });
            }

            try
            {
                var fileId = Guid.NewGuid().ToString();
                var filePath = await _fileService.CreateFileAsync(file, fileId, CurrentUserId);

                if (string.IsNullOrEmpty(filePath))
                {
                    TempData["Error"] = "File upload failed.";
                    return RedirectToAction("Index", "Folder");
                }

                var newFile = new StoredFile
                {
                    Id = fileId,
                    Name = file.FileName,
                    Size = (file.Length / 1024.0).ToString("F2") + " KB",
                    CreatedAt = DateTime.UtcNow,
                    WhoAdded = User.Identity?.Name ?? "Unknown",
                    FileExtension = Path.GetExtension(file.FileName),
                    FilePath = filePath,
                    IsStarred = false,
                    UserId = CurrentUserId,
                    FolderId = fileViewModel.CurrentFolderId
                };

                await _context.StoredFiles.AddAsync(newFile);
                await _context.SaveChangesAsync();

                _logger.LogInformation("File uploaded successfully: {FileId} by user {UserId}", fileId, CurrentUserId);
                TempData["Success"] = "File uploaded successfully.";
                return RedirectToAction("Index", new { id = fileViewModel.CurrentFolderId });
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Index", new { id = fileViewModel.CurrentFolderId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during file upload");
                TempData["Error"] = "An unexpected error occurred while uploading the file.";
                return RedirectToAction("Index", new { id = fileViewModel.CurrentFolderId });
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> DeleteFile(string id, string folderId)
        {
            var success = await _fileService.DeleteFileAsync(id, CurrentUserId);
            
            TempData[success ? "Success" : "Error"] = success 
                ? "File deleted successfully." 
                : "File not found or you don't have permission to delete it.";
                
            return RedirectToAction("Index", new { id = folderId });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ToggleStar(string id, string folderId)
        {
            var success = await _fileService.ToggleStarAsync(id, CurrentUserId);
            
            if (!success)
            {
                TempData["Error"] = "File not found or you don't have permission to modify it.";
            }
            
            return RedirectToAction("Index", new { id = folderId });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> RenameFile(string id, string newName, string folderId)
        {
            if (string.IsNullOrWhiteSpace(newName))
            {
                TempData["Error"] = "File name cannot be empty.";
                return RedirectToAction("Index", new { id = folderId });
            }

            var success = await _fileService.RenameFileAsync(id, newName, CurrentUserId);
            
            TempData[success ? "Success" : "Error"] = success 
                ? "File renamed successfully." 
                : "File not found or you don't have permission to rename it.";
                
            return RedirectToAction("Index", new { id = folderId });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> MoveFile(string id, string newFolderId, string currentFolderId)
        {
            var success = await _fileService.MoveFileAsync(id, newFolderId, CurrentUserId);
            
            TempData[success ? "Success" : "Error"] = success 
                ? "File moved successfully." 
                : "File or destination folder not found, or you don't have permission.";
                
            return RedirectToAction("Index", new { id = success ? newFolderId : currentFolderId });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> MoveFiles(string[] fileIds, string newFolderId, string currentFolderId)
        {
            if (fileIds == null || fileIds.Length == 0)
            {
                TempData["Error"] = "No files selected to move.";
                return RedirectToAction("Index", new { id = currentFolderId });
            }

            var success = await _fileService.MoveMultipleFilesAsync(fileIds, newFolderId, CurrentUserId);
            
            TempData[success ? "Success" : "Error"] = success 
                ? "Files moved successfully." 
                : "One or more files not found, destination folder not found, or insufficient permissions.";
                
            return RedirectToAction("Index", new { id = success ? newFolderId : currentFolderId });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> DeleteFiles(string[] fileIds, string currentFolderId)
        {
            if (fileIds == null || fileIds.Length == 0)
            {
                TempData["Error"] = "No files selected to delete.";
                return RedirectToAction("Index", new { id = currentFolderId });
            }

            var success = await _fileService.DeleteMultipleFilesAsync(fileIds, CurrentUserId);
            
            TempData[success ? "Success" : "Error"] = success 
                ? "Files deleted successfully." 
                : "One or more files not found or you don't have permission to delete them.";
                
            return RedirectToAction("Index", new { id = currentFolderId });
        }
    }
}