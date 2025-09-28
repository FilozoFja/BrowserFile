using BrowserFile.Data;
using BrowserFile.Models.Entities;
using BrowserFile.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scrypt;
using System.Security.Claims;
using BrowserFile.Interface;

namespace BrowserFile.Controllers
{
    public class SharingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private string CurrentUser => User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        private readonly ILogger<SharingController> _logger;
        private readonly IFileShareService _shareService;
        
        public SharingController(ApplicationDbContext context, 
                ILogger<SharingController> logger,
                IFileShareService shareService)
        {
            _context = context;
            _logger = logger;
            _shareService = shareService;
        }

        [Authorize]
        [HttpGet("share")]
        public async Task<IActionResult> Index()
        {
            var sharedFiles = await _shareService.GetSharedFiles(CurrentUser);
            var sharedLinks = await _shareService.GetSharedLinks(CurrentUser);

            List<ShareViewCombinedList> combinedList = new List<ShareViewCombinedList>();
            if (sharedFiles != null && sharedLinks != null)
            {
                var link = $"{Request.Scheme}://{Request.Host}";
                combinedList = _shareService.GetCombinedList(sharedFiles, sharedLinks, link);
            }
            
            var vm = new ShareViewModel
            {
                SharedCombinedList = combinedList
            };
            return View(vm);
        }

        [Authorize]
        [HttpGet("share/settings/{id}")]
        public async Task<IActionResult> ShareSettings(string id)
        {
            /*DODAC WYWOLANIE Z KONTROLLERA FILE DO TEGO*/
            var file = await _context.StoredFiles.FirstOrDefaultAsync(x => x.Id == id && x.UserId == CurrentUser);
            if (file == null)
            {
                TempData["ErrorMessage"] = "File not found or you do not have permission to edit its sharing settings.";
                return NotFound();
            }

            var activeLink = file.IsShared ? await _shareService.GetSharedLink(CurrentUser, id) : null;
            var sharingHistory = await _shareService.GetSharingHistory(CurrentUser, id);

            var vm = new ShareSettingsViewModel
            {
                File = file,
                SharedLink = activeLink,
                SharingHistory = sharingHistory.Any() ? sharingHistory : null,
                ExpirationDate = DateTime.Now.AddDays(1),
                ShareUrl = activeLink != null ? $"{Request.Scheme}://{Request.Host}/share/{activeLink.Token}" : null
            };
            
            return View(vm);
        }

        [Authorize]
        [HttpPost("share/settings/{id}")]
        public async Task<IActionResult> ShareNewFileLink(string id, ShareSettingsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var fileForView = await _shareService.GetSharedFile(CurrentUser, id);
                if (fileForView == null)
                {
                    TempData["ErrorMessage"] = "File not found.";
                    return RedirectToAction("Index");
                }

                model.File = fileForView;
                model.SharingHistory = await _shareService.GetSharingHistory(CurrentUser, id);
                TempData["ErrorMessage"] = "Something went wrong";
                return View("ShareSettings", model);
            }
            /*DODAC WYWOLANIE Z KONTROLLERA FILE DO TEGO*/
            var file = await _context.StoredFiles.FirstOrDefaultAsync(x => x.Id == id && x.UserId == CurrentUser);
            if (file == null)
            {
                TempData["ErrorMessage"] = "File not found or you do not have permission to edit its sharing settings.";
                return NotFound();
            }

            var existingLink = await _shareService.GetSharedLink(CurrentUser, id);
            if (existingLink != null)
            {
                try
                {
                    await _shareService.DeactivateSharedLink(CurrentUser, file.Id);
                }
                catch(Exception ex)
                {
                    TempData["ErrorMessage"] = "An error occurred while updating the existing link: ";
                    return RedirectToAction("ShareSettings", new { id = file.Id });
                }
            }

            var encoder = new ScryptEncoder();

            var newLink = new SharedLink
            {
                Id = Guid.NewGuid().ToString(),
                FileId = file.Id,
                Token = Guid.NewGuid().ToString(),
                ExpiresAt = (DateTime)(model.ExpirationDate != null ? model.ExpirationDate : DateTime.Now.AddDays(1)),
                OneTime = model.OneTime,
                PasswordHash = string.IsNullOrEmpty(model.Password) ? null : encoder.Encode(model.Password)
            };

            file.IsShared = true;
            _context.StoredFiles.Update(file);

            try
            {
                await _context.SharedLinks.AddAsync(newLink);
                await _context.SaveChangesAsync();
            }
            catch(Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while creating the sharing link";
                return RedirectToAction("ShareSettings", new { id = file.Id });
            }

            _logger.LogInformation("User {UserId} created a new sharing link for file {FileId}", CurrentUser, file.Id);
            TempData["SuccessMessage"] = "Sharing link created successfully.";
            return RedirectToAction("Index");
        }

        [Authorize]
        [HttpGet("share/settings/{id}/deactivate")]
        public async Task<IActionResult> DeactivateSharingLink(string id)
        {
            try
            {
                await _shareService.DeactivateSharedLink(CurrentUser, id);
                TempData["SuccessMessage"] = "Sharing link has been deactivated.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = "Shared link not found.";
                _logger.LogError(ex, "Sharing link not found for file {FileId} by user {UserId}", id, CurrentUser);
            }
            catch (DbUpdateException dbex)
            {
                TempData["ErrorMessage"] = "Sharing link could not be deactivated.";
                _logger.LogError(dbex, "Database error deactivating sharing link for file {FileId}", id);
            }
            return RedirectToAction("Index");
        }
    }
}
