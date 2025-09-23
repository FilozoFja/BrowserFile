using BrowserFile.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace BrowserFile.Controllers
{
    public class PublicFilesController : Controller
    {
        private readonly ILogger<PublicFilesController> _logger;
        private readonly ApplicationDbContext _context;

        public PublicFilesController(ILogger<PublicFilesController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        [HttpGet("share/{id}")]
        public async Task<IActionResult> Index(string id)
        {
            var fileSharing = await _context.SharedLinks
                        .Include(x => x.File)
                        .FirstOrDefaultAsync(x => (x.Token == id || x.Alias == id) 
                        && x.ExpiresAt > DateTime.Now 
                        && (x.OneTime == false 
                        || (x.OneTime == true && x.Used == 0)));

            if (fileSharing == null || fileSharing.File == null)
            {
                TempData["ErrorMessage"] = "Nice try.";
                return Redirect("https://www.youtube.com/watch?v=dQw4w9WgXcQ");
            }

            return View("Index",id);
        }

        [HttpGet("share/{id}/download")]
        public async Task<IActionResult> DownloadFile(string id, string? password)
        {
            var fileSharing = await _context.SharedLinks
                .Include(x => x.File)
                .FirstOrDefaultAsync(x => (x.Token == id || x.Alias == id)
                && x.ExpiresAt > DateTime.Now
                && (x.OneTime == false
                || (x.OneTime == true && x.Used == 0)));

            if (fileSharing == null || fileSharing.File == null)
            {
                TempData["ErrorMessage"] = "File is not existing.";
                return RedirectToAction("https://www.google.com/");
            }

            if(password == null && fileSharing.PasswordHash == null)
            {
                var stream = new FileStream(fileSharing.File.FilePath, FileMode.Open, FileAccess.Read);
                var contentType = GetContentType(fileSharing.File.FileExtension) ?? "application/octet-stream";

                _logger.LogInformation("File with id {FileId} downloaded by {UserId}", id, contentType);
                return File(stream, contentType, fileSharing.File.Name);
            }

            if(Encoder.Equals(password, fileSharing.PasswordHash))
            {
                var stream = new FileStream(fileSharing.File.FilePath, FileMode.Open, FileAccess.Read);
                var contentType = GetContentType(fileSharing.File.FileExtension) ?? "application/octet-stream";

                _logger.LogInformation("File with id {FileId} downloaded by {UserId}", id, contentType);
                return File(stream, contentType, fileSharing.File.Name);
            }
            else
            {
                var stream = new FileStream(fileSharing.File.FilePath, FileMode.Open, FileAccess.Read);
                var contentType = GetContentType(fileSharing.File.FileExtension) ?? "application/octet-stream";

                _logger.LogInformation("File with id {FileId} downloaded by {UserId}", id, contentType);
                return File(stream, contentType, fileSharing.File.Name);
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
