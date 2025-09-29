using BrowserFile.Models.Entities;
using BrowserFile.Models.DTO;

namespace BrowserFile.Interface
{
    public interface IFileService
    {
        // Existing methods (keep what you have)
        Task<StoredFile?> GetFileAsync(string id, string userId);
        string? GetSafeFilePath(string filePath);
        string? GetContentType(string? fileExtension);

        // New methods to add
        Task<string?> CreateFileAsync(IFormFile file, string fileId, string userId);
        Task<bool> DeleteFileAsync(string fileId, string userId);
        Task<bool> DeleteMultipleFilesAsync(string[] fileIds, string userId);
        Task<bool> RenameFileAsync(string fileId, string newName, string userId);
        Task<bool> MoveFileAsync(string fileId, string newFolderId, string userId);
        Task<bool> MoveMultipleFilesAsync(string[] fileIds, string newFolderId, string userId);
        Task<bool> ToggleStarAsync(string fileId, string userId);
        Task<List<StoredFile>> GetFilesByFolderAsync(string folderId, string userId);
        bool IsFileExtensionAllowed(string extension);
        bool IsFileSizeValid(long fileSize);
        Task<bool> VerifyFolderPermissionAsync(string folderId, string userId);
    }
}