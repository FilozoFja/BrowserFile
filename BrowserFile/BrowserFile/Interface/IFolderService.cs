using BrowserFile.Models.Entities;
using BrowserFile.Models.DTO;

namespace BrowserFile.Interface
{
    public interface IFolderService
    {
        Task<List<Folder>> GetUserFoldersAsync(string userId);
        Task<Folder?> GetFolderByIdAsync(string folderId, string userId);
        Task<Folder?> GetFolderWithFilesAsync(string folderId, string userId);
        Task<bool> CreateFolderAsync(FolderDTO folderDto, string userId);
        Task<bool> UpdateFolderAsync(string folderId, FolderDTO folderDto, string userId);
        Task<bool> DeleteFolderAsync(string folderId, string userId);
        Task<bool> FolderHasFilesAsync(string folderId, string userId);
        Task<bool> ValidateIconExistsAsync(string iconId);
        Task<List<Icon>> GetIconsAsync();
    }
}