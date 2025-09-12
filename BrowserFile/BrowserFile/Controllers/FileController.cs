using BrowserFile.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using BrowserFile.Models.ViewModels;
using BrowserFile.Models.Entities;

namespace BrowserFile.Controllers
{
    public class FileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private string CurrentUserId => User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;


        public FileController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
            _context = context;
        }

        [HttpGet]
        [Authorize]
        public IActionResult Index([FromRoute] string id)
        {
            var files = _context.StoredFiles.Where(f => f.UserId == CurrentUserId && f.FolderId == id).ToList();
            var folder = _context.StoredFiles.FirstOrDefault(f => f.Id == id && f.UserId == CurrentUserId);
            if (folder == null && !string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "Folder not found.";
                return RedirectToAction("Index", "Folder");
            }

            var vm = new FileViewModel
            {
                Files = files,
                CurrentFolderId = id,
                FolderName = folder?.Name ?? "Root"
            };
            return View(vm);
        }

        [HttpPost]
        [Authorize]
        public IActionResult CreateFolder(FileViewModel fileViewModel, IFormFile file)
        {
            var folder = _context.FirstOrDefault<Folder>(f => f.Id == fileViewModel.CurrentFolderId && f.UserId == CurrentUserId);
            if (folder.UserId != CurrentUserId)
            {
                TempData["Error"] = "You do not have permission to create files in this folder.";
                return RedirectToAction("Index", "Folder");
            }

            if (fileViewModel.FileToCreate == null || string.IsNullOrWhiteSpace(fileViewModel.FileToCreate.Name))
            {
                TempData["Error"] = "Folder name is required.";
                return RedirectToAction("Index", new { id = fileViewModel.CurrentFolderId });
            }
            var fileId =  Guid.NewGuid().ToString();
            var filePath = FileHandler(file, fileId);

            if (filePath == null)
            {
                TempData["Error"] = "File upload failed.";
                return RedirectToAction("Index", "Folder");
            }

            var newFile = new StoredFile
            {
                Id = fileId,
                Name = fileViewModel.FileToCreate.Name,
                Size = (file.Length / 1024.0).ToString("F2") + " KB",
                CreatedAt = DateTime.UtcNow,
                WhoAdded = User.Identity?.Name ?? "Unknown",
                FileExtension = Path.GetExtension(file.FileName),
                FilePath = filePath,
                IsStarred = fileViewModel.FileToCreate.IsStarred,
                UserId = CurrentUserId,
                FolderId = fileViewModel.CurrentFolderId
            };

            try
            {
                _context.StoredFiles.Add(newFile);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while creating the file: " + ex.Message;
                return RedirectToAction("Index", new { id = fileViewModel.CurrentFolderId });
            }

            TempData["Success"] = "File created successfully.";
            return RedirectToAction("Index", new { id = fileViewModel.CurrentFolderId });
        }
        
        public async Task<string?> FileHandler(IFormFile file, string fileId, string userId)
        {
            if (file == null || file.Length == 0)
                return null;

            const long maxFileSize = 100 * 1024 * 1024; 
            if (file.Length > maxFileSize)
                throw new InvalidOperationException($"File size exceeds {maxFileSize / 1024 / 1024}MB limit");

            var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            var dangerousExtensions = new[] { ".exe", ".bat", ".cmd", ".scr", ".pif", ".com", ".vbs", ".ps1" };
            
            if (!string.IsNullOrEmpty(extension) && dangerousExtensions.Contains(extension))
                throw new InvalidOperationException($"File type '{extension}' is blocked for security");

            try
            {
                var uploadPath = _configuration.GetValue<string>("FileUpload:BasePath") ?? "uploads";
                
                var userFolder = Path.Combine(uploadPath, userId);
                var fullUserPath = Path.Combine(Directory.GetCurrentDirectory(), userFolder);
                
                Directory.CreateDirectory(fullUserPath);

                var safeFileName = $"{fileId}{extension}";
                var filePath = Path.Combine(fullUserPath, safeFileName);

                using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
                await file.CopyToAsync(stream);

                return Path.Combine(userFolder, safeFileName);
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException($"File upload failed: {ex.Message}");
            }
        }

        private bool IsValidImageFile(IFormFile file)
        {
            try
            {
                using var stream = file.OpenReadStream();
                using var image = System.Drawing.Image.FromStream(stream);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
            return sanitized.Length > 100 ? sanitized.Substring(0, 100) : sanitized;
        }
    }
}
