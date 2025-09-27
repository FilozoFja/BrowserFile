using BrowserFile.Data;
using BrowserFile.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scrypt;
using System.Security.Cryptography;

namespace BrowserFile.Controllers
{
    public class PublicFilesController : Controller
    {
        private readonly ILogger<PublicFilesController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public PublicFilesController(ILogger<PublicFilesController> logger,
                                    ApplicationDbContext context,
                                    IWebHostEnvironment environment)
        {
            _logger = logger;
            _context = context;
            _environment = environment;
        }

        [HttpGet("share/{token}")]
        public async Task<IActionResult> Index(string token)
        {
            if (string.IsNullOrWhiteSpace(token) || !IsValidToken(token))
            {
                return NotFound();
            }

            var fileSharing = await GetValidSharedLinkAsync(token);

            if (fileSharing == null)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100 + RandomNumberGenerator.GetInt32(0, 50)));
                return NotFound();
            }

            var viewModel = new PublicFileViewModel
            {
                Token = token,
                FileName = fileSharing.File.Name,
                FileSize = fileSharing.File.Size,
                IsRequiredPassword = !string.IsNullOrEmpty(fileSharing.PasswordHash),
                IsOneTime = fileSharing.OneTime
            };

            return View(viewModel);
        }

        [HttpGet("share/{token}/download")]
        public async Task<IActionResult> DownloadFile(string token, string? password)
        {
            if (string.IsNullOrWhiteSpace(token) || !IsValidToken(token))
            {
                return NotFound();
            }

            var fileSharing = await GetValidSharedLinkAsync(token);

            if (fileSharing == null || fileSharing.File == null)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100 + RandomNumberGenerator.GetInt32(0, 50)));
                return NotFound();
            }

            if (fileSharing.HasPassword)
            {
                if (string.IsNullOrEmpty(password))
                {
                    TempData["ErrorMessage"] = "Password is required to download this file.";
                    return RedirectToAction("Index", new { token = token });
                }

                var encoder = new ScryptEncoder();
                if (!encoder.Compare(password, fileSharing.PasswordHash))
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));

                    _logger.LogWarning("Invalid password attempt for shared link {Token} from IP {IP}",
                        token, HttpContext.Connection.RemoteIpAddress);

                    TempData["ErrorMessage"] = "Invalid password.";
                    return RedirectToAction("Index", new { token });
                }
            }
            var fileResult = await GetSecureFileAsync(fileSharing.File);

            if (fileResult == null)
            {
                _logger.LogError("File not found on disk for shared link {Token}: {FilePath}", 
                    token, fileSharing.File.FilePath);
                TempData["ErrorMessage"] = "File is no longer available.";
                return RedirectToAction("Index", new { token });
            }

            if (fileSharing.OneTime)
            {
                await MarkAsUsedAsync(fileSharing);
            }

            _logger.LogInformation("File {FileName} (ID: {FileId}) downloaded via shared link {Token} from IP {IP}", 
                fileSharing.File.Name, fileSharing.File.Id, token, HttpContext.Connection.RemoteIpAddress);

            var contentType = GetContentType(fileSharing.File.FileExtension);
            
            return File(fileResult.FileStream, contentType, fileSharing.File.Name, enableRangeProcessing: true);

        }

        private async Task MarkAsUsedAsync(SharedLink sharedLink)
        {
            try
            {
                sharedLink.Used = 1;
                sharedLink.ExpiresAt = DateTime.Now;
                _context.SharedLinks.Update(sharedLink);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark shared link {Token} as used", sharedLink.Token);
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

        private async Task<SharedLink?> GetValidSharedLinkAsync(string token)
        {
            return await _context.SharedLinks
                        .Include(x => x.File)
                        .FirstOrDefaultAsync(x => (x.Token == token || x.Alias == token)
                        && x.ExpiresAt > DateTime.Now
                        && (x.OneTime == false
                        || (x.OneTime == true && x.Used == 0)));
        }

        private static bool IsValidToken(string token)
        {
            return token.Length <= 100 &&
                    token.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_');
        }

        private async Task<SecureFileResult?> GetSecureFileAsync(StoredFile file)
        {
            try
            {
                var safePath = GetSafeFilePath(file.FilePath);
                if (safePath == null)
                {
                    _logger.LogWarning("Potential path traversal attempt: {FilePath}", file.FilePath);
                    return null;
                }

                if (!System.IO.File.Exists(safePath))
                {
                    return null;
                }

                var fileStream = new FileStream(safePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                return new SecureFileResult { FileStream = fileStream };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accessing file {FilePath}", file.FilePath);
                return null;
            }
        }

        public class SecureFileResult
        {
            public FileStream FileStream { get; set; } = null!;
        }
        
        private string? GetSafeFilePath(string filePath)
        {
            try
            {
                var uploadsPath = Path.Combine(_environment.ContentRootPath, "uploads");

                var normalizedPath = Path.GetFullPath(Path.Combine(uploadsPath, filePath));

                if (!normalizedPath.StartsWith(Path.GetFullPath(uploadsPath)))
                {
                    return null; 
                }

                return normalizedPath;
            }
            catch
            {
                return null;
            }
        }
    }
}
