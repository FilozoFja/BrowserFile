using BrowserFile.Interface;
using BrowserFile.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;

namespace BrowserFile.Controllers
{
    public class PublicFilesController : Controller
    {
        private readonly ILogger<PublicFilesController> _logger;
        private readonly IPublicFileService _publicFileService;
        private readonly IFileService _fileService;

        public PublicFilesController(ILogger<PublicFilesController> logger,
                                    IPublicFileService publicFileService,
                                    IFileService fileService)
        {
            _logger = logger;
            _publicFileService = publicFileService;
            _fileService = fileService;
        }

        [HttpGet("share/{token}")]
        public async Task<IActionResult> Index(string token)
        {
            if (!_publicFileService.IsValidToken(token))
            {
                return NotFound();
            }

            var fileSharing = await _publicFileService.GetValidSharedLinkAsync(token);

            if (fileSharing == null)
            {
                // Add random delay to prevent timing attacks
                await Task.Delay(TimeSpan.FromMilliseconds(100 + RandomNumberGenerator.GetInt32(0, 50)));
                return NotFound();
            }

            var viewModel = new PublicFileViewModel
            {
                Token = token,
                FileName = fileSharing.File.Name,
                FileSize = fileSharing.File.Size,
                IsRequiredPassword = !string.IsNullOrEmpty(fileSharing.PasswordHash),
                IsOneTime = fileSharing.OneTime,
                FileExtension = fileSharing.File.FileExtension
            };

            return View(viewModel);
        }

        [HttpGet("share/{token}/download")]
        public async Task<IActionResult> DownloadFile(string token, string? password)
        {
            if (!_publicFileService.IsValidToken(token))
            {
                return NotFound();
            }

            var fileSharing = await _publicFileService.GetValidSharedLinkAsync(token);

            if (fileSharing == null || fileSharing.File == null)
            {
                // Add random delay to prevent timing attacks
                await Task.Delay(TimeSpan.FromMilliseconds(100 + RandomNumberGenerator.GetInt32(0, 50)));
                return NotFound();
            }

            // Validate password if required
            if (fileSharing.HasPassword)
            {
                if (string.IsNullOrEmpty(password))
                {
                    TempData["ErrorMessage"] = "Password is required to download this file.";
                    return RedirectToAction("Index", new { token });
                }

                if (!await _publicFileService.ValidatePasswordAsync(password, fileSharing.PasswordHash))
                {
                    // Add delay to prevent brute force attacks
                    await Task.Delay(TimeSpan.FromSeconds(1));

                    _logger.LogWarning("Invalid password attempt for shared link {Token} from IP {IP}",
                        token, HttpContext.Connection.RemoteIpAddress);

                    TempData["ErrorMessage"] = "Invalid password.";
                    return RedirectToAction("Index", new { token });
                }
            }

            // Get file stream
            var fileStream = await _publicFileService.GetSecureFileStreamAsync(fileSharing.File);

            if (fileStream == null)
            {
                _logger.LogError("File not found on disk for shared link {Token}: {FilePath}", 
                    token, fileSharing.File.FilePath);
                TempData["ErrorMessage"] = "File is no longer available.";
                return RedirectToAction("Index", new { token });
            }

            // Mark as used if one-time link
            if (fileSharing.OneTime)
            {
                await _publicFileService.MarkSharedLinkAsUsedAsync(fileSharing);
            }

            _logger.LogInformation("File {FileName} (ID: {FileId}) downloaded via shared link {Token} from IP {IP}", 
                fileSharing.File.Name, fileSharing.File.Id, token, HttpContext.Connection.RemoteIpAddress);

            var contentType = _fileService.GetContentType(fileSharing.File.FileExtension) ?? "application/octet-stream";
            
            return File(fileStream, contentType, fileSharing.File.Name, enableRangeProcessing: true);
        }
    }
}