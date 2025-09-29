using BrowserFile.Data;
using BrowserFile.Interface;
using BrowserFile.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BrowserFile.Services
{
    public class FileService : IFileService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FileService> _logger;

        public FileService(ApplicationDbContext context, IConfiguration configuration, ILogger<FileService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<StoredFile?> GetFileAsync(string id, string userId)
        {
            return await _context.StoredFiles.FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);
        }

        public string? GetSafeFilePath(string filePath)
        {
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), filePath);
            return File.Exists(fullPath) ? fullPath : null;
        }

        public string? GetContentType(string fileExtension)
        {
            return fileExtension?.ToLower() switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".txt" => "text/plain",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                _ => "application/octet-stream"
            };
        }

        public async Task<string?> CreateFileAsync(IFormFile file, string fileId, string userId)
        {
            if (file == null) return null;

            if (!IsFileSizeValid(file.Length))
                throw new InvalidOperationException("File size exceeds 100MB limit");

            var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (!IsFileExtensionAllowed(extension))
                throw new InvalidOperationException($"File type '{extension}' is blocked for security reasons");

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

                _logger.LogInformation("File created successfully: {FilePath} for user {UserId}", filePath, userId);
                return Path.Combine(userFolder, safeFileName);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Failed to create file for user {UserId}", userId);
                throw new InvalidOperationException($"File upload failed: {ex.Message}");
            }
        }

        public async Task<bool> DeleteFileAsync(string fileId, string userId)
        {
            var file = await GetFileAsync(fileId, userId);
            if (file == null) return false;

            try
            {
                var fullFilePath = Path.Combine(Directory.GetCurrentDirectory(), file.FilePath);
                if (File.Exists(fullFilePath))
                {
                    File.Delete(fullFilePath);
                }

                _context.StoredFiles.Remove(file);
                await _context.SaveChangesAsync();

                _logger.LogInformation("File deleted successfully: {FileId} by user {UserId}", fileId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete file {FileId} for user {UserId}", fileId, userId);
                return false;
            }
        }

        public async Task<bool> DeleteMultipleFilesAsync(string[] fileIds, string userId)
        {
            if (fileIds == null || fileIds.Length == 0) return false;

            var files = await _context.StoredFiles
                .Where(f => fileIds.Contains(f.Id) && f.UserId == userId)
                .ToListAsync();

            if (files.Count != fileIds.Length) return false;

            try
            {
                foreach (var file in files)
                {
                    var fullFilePath = Path.Combine(Directory.GetCurrentDirectory(), file.FilePath);
                    if (File.Exists(fullFilePath))
                    {
                        File.Delete(fullFilePath);
                    }
                }

                _context.StoredFiles.RemoveRange(files);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Multiple files deleted successfully by user {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete multiple files for user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> RenameFileAsync(string fileId, string newName, string userId)
        {
            if (string.IsNullOrWhiteSpace(newName)) return false;

            var file = await GetFileAsync(fileId, userId);
            if (file == null) return false;

            try
            {
                file.Name = newName;
                _context.StoredFiles.Update(file);
                await _context.SaveChangesAsync();

                _logger.LogInformation("File renamed successfully: {FileId} to {NewName} by user {UserId}", fileId, newName, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to rename file {FileId} for user {UserId}", fileId, userId);
                return false;
            }
        }

        public async Task<bool> MoveFileAsync(string fileId, string newFolderId, string userId)
        {
            var file = await GetFileAsync(fileId, userId);
            if (file == null) return false;

            if (!await VerifyFolderPermissionAsync(newFolderId, userId)) return false;

            try
            {
                file.FolderId = newFolderId;
                _context.StoredFiles.Update(file);
                await _context.SaveChangesAsync();

                _logger.LogInformation("File moved successfully: {FileId} to folder {FolderId} by user {UserId}", fileId, newFolderId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to move file {FileId} for user {UserId}", fileId, userId);
                return false;
            }
        }

        public async Task<bool> MoveMultipleFilesAsync(string[] fileIds, string newFolderId, string userId)
        {
            if (fileIds == null || fileIds.Length == 0) return false;

            var files = await _context.StoredFiles
                .Where(f => fileIds.Contains(f.Id) && f.UserId == userId)
                .ToListAsync();

            if (files.Count != fileIds.Length) return false;

            if (!await VerifyFolderPermissionAsync(newFolderId, userId)) return false;

            try
            {
                foreach (var file in files)
                {
                    file.FolderId = newFolderId;
                }
                
                _context.StoredFiles.UpdateRange(files);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Multiple files moved successfully to folder {FolderId} by user {UserId}", newFolderId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to move multiple files for user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> ToggleStarAsync(string fileId, string userId)
        {
            var file = await GetFileAsync(fileId, userId);
            if (file == null) return false;

            try
            {
                file.IsStarred = !file.IsStarred;
                _context.StoredFiles.Update(file);
                await _context.SaveChangesAsync();

                _logger.LogInformation("File star toggled: {FileId} by user {UserId}", fileId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to toggle star for file {FileId} for user {UserId}", fileId, userId);
                return false;
            }
        }

        public async Task<List<StoredFile>> GetFilesByFolderAsync(string folderId, string userId)
        {
            return await _context.StoredFiles
                .Where(f => f.UserId == userId && f.FolderId == folderId)
                .ToListAsync();
        }

        public bool IsFileExtensionAllowed(string? extension)
        {
            if (string.IsNullOrEmpty(extension)) return false;

            var dangerousExtensions = new[] { ".exe", ".bat", ".cmd", ".scr", ".pif", ".com", ".vbs", ".ps1" };
            return !dangerousExtensions.Contains(extension.ToLowerInvariant());
        }

        public bool IsFileSizeValid(long fileSize)
        {
            const long maxFileSize = 100 * 1024 * 1024; // 100MB
            return fileSize <= maxFileSize;
        }

        public async Task<bool> VerifyFolderPermissionAsync(string folderId, string userId)
        {
            return await _context.Folders.AnyAsync(f => f.Id == folderId && f.UserId == userId);
        }
    }
}