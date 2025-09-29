using BrowserFile.Models.Entities;

namespace BrowserFile.Interface
{
    public interface IPublicFileService
    {
        Task<SharedLink?> GetValidSharedLinkAsync(string token);
        Task<bool> ValidatePasswordAsync(string password, string passwordHash);
        Task<FileStream?> GetSecureFileStreamAsync(StoredFile file);
        Task MarkSharedLinkAsUsedAsync(SharedLink sharedLink);
        bool IsValidToken(string token);
        string? GetSafeFilePath(string filePath);
    }
}