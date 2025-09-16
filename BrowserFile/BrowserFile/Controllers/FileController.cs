using BrowserFile.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using BrowserFile.Models.ViewModels;
using BrowserFile.Models.Entities;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using BrowserFile.Models.DTO;

namespace BrowserFile.Controllers
{
    public class FileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private string CurrentUserId => User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        private readonly IMapper _mapper;

        public FileController(ApplicationDbContext context, IConfiguration configuration, IMapper mapper)
        {
            _context = context;
            _configuration = configuration;
            _mapper = mapper;
        }

        [HttpGet]
        [Authorize]
        public IActionResult Index([FromRoute] string id)
        {
            var files = _context.StoredFiles.Where(f => f.UserId == CurrentUserId && f.FolderId == id).ToList();
            var folders = _context.Folders.Where(f => f.UserId == CurrentUserId).ToList();
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
            var file = await _context.StoredFiles.FirstOrDefaultAsync(f => f.Id == id && f.UserId == CurrentUserId);
            if (file == null)
            {
                TempData["Error"] = "File not found.";
                return RedirectToAction("Index", "Folder");
            }

            var fullFilePath = Path.Combine(Directory.GetCurrentDirectory(), file.FilePath);
            if (!System.IO.File.Exists(fullFilePath))
            {
                TempData["Error"] = "File not found on server.";
                return RedirectToAction("Index", "Folder");
            }

            var stream = new FileStream(fullFilePath, FileMode.Open, FileAccess.Read);
            var contentType = GetContentType(file.FileExtension) ?? "application/octet-stream";

            return File(stream, contentType, file.Name);
        }
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Preview(string id)
        {
            var file = await _context.StoredFiles.FirstOrDefaultAsync(f => f.Id == id && f.UserId == CurrentUserId);
            if (file == null)
            {
                TempData["Error"] = "File not found.";
                return RedirectToAction("Index", "Folder");
            }

            var previewableExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".gif", ".txt" };

            if (!previewableExtensions.Contains(file.FileExtension?.ToLower()))
            {
                TempData["Error"] = "Nie można podglądać tego typu pliku";
                return RedirectToAction("Index", "Folder");
            }

            var fullFilePath = Path.Combine(Directory.GetCurrentDirectory(), file.FilePath);
            if (!System.IO.File.Exists(fullFilePath))
            {
                TempData["Error"] = "File not found on server.";
                return RedirectToAction("Index", "Folder");
            }
            var stream = new FileStream(fullFilePath, FileMode.Open, FileAccess.Read);
            var contentType = GetContentType(file.FileExtension) ?? "application/octet-stream";
            return File(stream, contentType);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UploadFile(FileViewModel fileViewModel, IFormFile file)
        {
            var folder = await _context.Folders.FirstOrDefaultAsync(f => f.Id == fileViewModel.CurrentFolderId && f.UserId == CurrentUserId);

            if (folder == null)
            {
                TempData["Error"] = "Folder not found.";
                return RedirectToAction("Index", "Folder");
            }

            if (folder.UserId != CurrentUserId)
            {
                TempData["Error"] = "You do not have permission to create files in this folder.";
                return RedirectToAction("Index", "Folder");
            }
            
            if (file.FileName == null || string.IsNullOrWhiteSpace(file.FileName))
            {
                TempData["Error"] = "File name is required.";
                return RedirectToAction("Index", new { id = fileViewModel.CurrentFolderId });
            }

            var fileId =  Guid.NewGuid().ToString();
            var filePath = await FileCreateHandler(file, fileId);

            if (filePath == null || string.IsNullOrEmpty(filePath))
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

            try
            {
                await _context.StoredFiles.AddAsync(newFile);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while creating the file: " + ex.Message;
                return RedirectToAction("Index", new { id = fileViewModel.CurrentFolderId });
            }

            TempData["Success"] = "File created successfully.";
            return RedirectToAction("Index", new { id = fileViewModel.CurrentFolderId });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> DeleteFile(string id, string folderId)
        {
            var file = await _context.StoredFiles.FirstOrDefaultAsync(f => f.Id == id && f.UserId == CurrentUserId);
            if (file == null)
            {
                TempData["Error"] = "File not found.";
                return RedirectToAction("Index", new { id = folderId });
            }

            if (file.UserId != CurrentUserId)
            {
                TempData["Error"] = "You do not have permission to delete this file.";
                return RedirectToAction("Index", new { id = folderId });
            }

            try
            {
                var fullFilePath = Path.Combine(Directory.GetCurrentDirectory(), file.FilePath);
                if (System.IO.File.Exists(fullFilePath))
                {
                    System.IO.File.Delete(fullFilePath);
                }

                _context.StoredFiles.Remove(file);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while deleting the file: " + ex.Message;
                return RedirectToAction("Index", new { id = folderId });
            }

            TempData["Success"] = "File deleted successfully.";
            return RedirectToAction("Index", new { id = folderId });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ToggleStar(string id, string folderId)
        {
            var file = await _context.StoredFiles.FirstOrDefaultAsync(f => f.Id == id && f.UserId == CurrentUserId);
            if (file == null)
            {
                TempData["Error"] = "File not found.";
                return RedirectToAction("Index", new { id = folderId });
            }

            if (file.UserId != CurrentUserId)
            {
                TempData["Error"] = "You do not have permission to modify this file.";
                return RedirectToAction("Index", new { id = folderId });
            }

            try
            {
                file.IsStarred = !file.IsStarred;
                _context.StoredFiles.Update(file);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while updating the file: " + ex.Message;
                return RedirectToAction("Index", new { id = folderId });
            }

            TempData["Success"] = file.IsStarred ? "File starred." : "File unstarred.";
            return RedirectToAction("Index", new { id = folderId });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> RenameFile(string id, string newName, string folderId)
        {
            var file = await _context.StoredFiles.FirstOrDefaultAsync(f => f.Id == id && f.UserId == CurrentUserId);
            if (file == null)
            {
                TempData["Error"] = "File not found.";
                return RedirectToAction("Index", new { id = folderId });
            }

            if (file.UserId != CurrentUserId)
            {
                TempData["Error"] = "You do not have permission to rename this file.";
                return RedirectToAction("Index", new { id = folderId });
            }

            if (string.IsNullOrWhiteSpace(newName))
            {
                TempData["Error"] = "New file name cannot be empty.";
                return RedirectToAction("Index", new { id = folderId });
            }

            try
            {
                file.Name = newName;
                _context.StoredFiles.Update(file);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while renaming the file: " + ex.Message;
                return RedirectToAction("Index", new { id = folderId });
            }

            TempData["Success"] = "File renamed successfully.";
            return RedirectToAction("Index", new { id = folderId });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> MoveFile(string id, string newFolderId, string currentFolderId)
        {
            var file = await _context.StoredFiles.FirstOrDefaultAsync(f => f.Id == id && f.UserId == CurrentUserId);
            if (file == null)
            {
                TempData["Error"] = "File not found.";
                return RedirectToAction("Index", new { id = currentFolderId });
            }
            if (file.UserId != CurrentUserId)
            {
                TempData["Error"] = "You do not have permission to move this file.";
                return RedirectToAction("Index", new { id = currentFolderId });
            }
            var newFolder = await _context.Folders.FirstOrDefaultAsync(f => f.Id == newFolderId && f.UserId == CurrentUserId);
            if (newFolder == null)
            {
                TempData["Error"] = "Destination folder not found.";
                return RedirectToAction("Index", new { id = currentFolderId });
            }
            try
            {
                file.FolderId = newFolderId;
                _context.StoredFiles.Update(file);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while moving the file: " + ex.Message;
                return RedirectToAction("Index", new { id = currentFolderId });
            }
            TempData["Success"] = "File moved successfully.";
            return RedirectToAction("Index", new { id = newFolderId });
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

            var files = await _context.StoredFiles.Where(f => fileIds.Contains(f.Id) && f.UserId == CurrentUserId).ToListAsync();
            if (files.Count != fileIds.Length)
            {
                TempData["Error"] = "One or more files not found or you do not have permission to move them.";
                return RedirectToAction("Index", new { id = currentFolderId });
            }

            var newFolder = await _context.Folders.FirstOrDefaultAsync(f => f.Id == newFolderId && f.UserId == CurrentUserId);
            if (newFolder == null)
            {
                TempData["Error"] = "Destination folder not found.";
                return RedirectToAction("Index", new { id = currentFolderId });
            }

            try
            {
                foreach (var file in files)
                {
                    file.FolderId = newFolderId;
                }
                _context.StoredFiles.UpdateRange(files);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while moving the files: " + ex.Message;
                return RedirectToAction("Index", new { id = currentFolderId });
            }

            TempData["Success"] = "Files moved successfully.";
            return RedirectToAction("Index", new { id = newFolderId });
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

            var files = await _context.StoredFiles.Where(f => fileIds.Contains(f.Id) && f.UserId == CurrentUserId).ToListAsync();
            if (files.Count != fileIds.Length)
            {
                TempData["Error"] = "One or more files not found or you do not have permission to delete them.";
                return RedirectToAction("Index", new { id = currentFolderId });
            }

            try
            {
                foreach (var file in files)
                {
                    var fullFilePath = Path.Combine(Directory.GetCurrentDirectory(), file.FilePath);
                    if (System.IO.File.Exists(fullFilePath))
                    {
                        System.IO.File.Delete(fullFilePath);
                    }
                }

                _context.StoredFiles.RemoveRange(files);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while deleting the files: " + ex.Message;
                return RedirectToAction("Index", new { id = currentFolderId });
            }

            TempData["Success"] = "Files deleted successfully.";
            return RedirectToAction("Index", new { id = currentFolderId });
        }

        public async Task<string?> FileCreateHandler(IFormFile file, string fileId)
        {
            if (file == null)
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

                var userFolder = Path.Combine(uploadPath, CurrentUserId);
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

        private string GetContentType(string extension)
        {
            return extension?.ToLower() switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".zip" => "application/zip",
                ".rar" => "application/x-rar-compressed",
                ".txt" => "text/plain",
                _ => "application/octet-stream"
            };
        }
    }
}
