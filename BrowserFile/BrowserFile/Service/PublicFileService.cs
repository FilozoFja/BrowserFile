using BrowserFile.Data;
using BrowserFile.Interface;
using BrowserFile.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Scrypt;

namespace BrowserFile.Service
{
    public class PublicFileService : IPublicFileService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<PublicFileService> _logger;

        public PublicFileService(ApplicationDbContext context, 
                                IWebHostEnvironment environment, 
                                ILogger<PublicFileService> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        public async Task<SharedLink?> GetValidSharedLinkAsync(string token)
        {
            return await _context.SharedLinks
                .Include(x => x.File)
                .FirstOrDefaultAsync(x => 
                    (x.Token == token || x.Alias == token) &&
                    x.ExpiresAt.AddSeconds(5) > DateTime.Now &&
                    (x.OneTime == false || (x.OneTime == true && x.Used == 0)));
        }

        public async Task<bool> ValidatePasswordAsync(string password, string passwordHash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(passwordHash))
            {
                return false;
            }

            try
            {
                var encoder = new ScryptEncoder();
                return encoder.Compare(password, passwordHash);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating password hash");
                return false;
            }
        }

        public async Task<FileStream?> GetSecureFileStreamAsync(StoredFile file)
        {
            try
            {
                var safePath = GetSafeFilePath(file.FilePath);
                if (safePath == null)
                {
                    _logger.LogWarning("Potential path traversal attempt: {FilePath}", file.FilePath);
                    return null;
                }

                if (!File.Exists(safePath))
                {
                    _logger.LogWarning("File not found on disk: {FilePath}", safePath);
                    return null;
                }

                var fileStream = new FileStream(safePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                return fileStream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accessing file {FilePath}", file.FilePath);
                return null;
            }
        }

        public async Task MarkSharedLinkAsUsedAsync(SharedLink sharedLink)
        {
            try
            {
                sharedLink.Used = 1;
                sharedLink.ExpiresAt = DateTime.Now;
                _context.SharedLinks.Update(sharedLink);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Shared link {Token} marked as used", sharedLink.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark shared link {Token} as used", sharedLink.Token);
                throw;
            }
        }

        public bool IsValidToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            return token.Length <= 100 &&
                   token.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_');
        }

        public string? GetSafeFilePath(string filePath)
        {
            try
            {
                var universalPath = _environment.ContentRootPath;
                filePath = filePath.Replace(@"\", "/");
                
                var uploadsPath = Path.Combine(_environment.ContentRootPath, "uploads");
                var normalizedPath = Path.GetFullPath(Path.Combine(universalPath, filePath));

                if (!normalizedPath.StartsWith(Path.GetFullPath(uploadsPath)))
                {
                    _logger.LogWarning("Path outside uploads directory: {Path}", normalizedPath);
                    return null; 
                }
                
                return normalizedPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error normalizing file path: {FilePath}", filePath);
                return null;
            }
        }
    }
}