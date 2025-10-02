using BrowserFile.Models.Entities;

namespace BrowserFile.Interface
{
    public interface IStorageService
    {
        Task<List<Icon>> GetIconsAsync();
        Task<bool> ValidateIconExistsAsync(string iconId);
        Task<bool> UserHasPermissionToFolderAsync(string folderId, string userId);
        Task<Folder?> GetFolderWithDetailsAsync(string folderId, string userId);
        Task<int> GetFolderFileCountAsync(string folderId, string userId);
        Task<long> GetFolderSizeAsync(string folderId, string userId);
    }
}