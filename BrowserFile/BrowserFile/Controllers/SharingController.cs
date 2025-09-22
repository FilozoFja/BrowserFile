using BrowserFile.Data;
using BrowserFile.Models.Entities;
using BrowserFile.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scrypt;
using System.Security.Claims;

namespace BrowserFile.Controllers
{
    public class SharingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private string CurrentUser => User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        
        public SharingController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var sharedFiles = await _context.StoredFiles
                .Include(f => f.SharedLink)
                .Where(x => x.UserId == CurrentUser 
                    && x.IsShared 
                    && x.SharedLink != null)
                .ToListAsync();
            return View(sharedFiles);
        }

        [Authorize]
        [HttpGet("share/settings/{id}")]
        public async Task<IActionResult> ShareSettings(string id)
        {
            var file = await _context.StoredFiles.FirstOrDefaultAsync(x => x.Id == id && x.UserId == CurrentUser);
            if (file == null)
            {
                TempData["ErrorMessage"] = "File not found or you do not have permission to edit its sharing settings.";
                return NotFound();
            }

            var activeLink = file.IsShared ? await _context.SharedLinks
                .FirstOrDefaultAsync(x => x.FileId == file.Id && x.ExpiresAt > DateTime.Now) : null;

            var sharingHistory = await _context.SharedLinks
                .Where(x => x.FileId == file.Id)
                .OrderByDescending(x => x.ExpiresAt)
                .Take(10) 
                .ToListAsync();

            var vm = new ShareSettingsViewModel
            {
                File = file,
                SharedLink = activeLink,
                SharingHistory = sharingHistory,
                ExpirationDate = DateTime.Now.AddDays(1)
            };

            if (activeLink != null)
            {
                vm.ShareUrl = Url.Action("Download", "SharedFiles",
                    new { token = activeLink.Token }, Request.Scheme);
            }

            return View(vm);
        }

        [Authorize]
        [HttpPost("share/settings/{id}")]
        public async Task<IActionResult> ShareNewFileLink(string id, ShareSettingsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var fileForView = await _context.StoredFiles.FirstOrDefaultAsync(x => x.Id == id && x.UserId == CurrentUser);
                if (fileForView == null)
                {
                    TempData["ErrorMessage"] = "File not found.";
                    return RedirectToAction("Index");
                }

                model.File = fileForView;
                model.SharingHistory = await _context.SharedLinks
                    .Where(x => x.FileId == fileForView.Id)
                    .OrderByDescending(x => x.ExpiresAt)
                    .Take(10)
                    .ToListAsync();
                TempData["ErrorMessage"] = "Something went wrong";
                return View("ShareSettings", model);
            }

            var file = await _context.StoredFiles.FirstOrDefaultAsync(x => x.Id == id && x.UserId == CurrentUser);

            if (file == null)
            {
                TempData["ErrorMessage"] = "File not found or you do not have permission to edit its sharing settings.";
                return NotFound();
            }

            var existingLink = await _context.SharedLinks.FirstOrDefaultAsync(x => x.FileId == file.Id && x.ExpiresAt > DateTime.Now);

            if (existingLink != null)
            {
                existingLink.ExpiresAt = DateTime.Now;

                try
                {
                    _context.SharedLinks.Update(existingLink);
                    await _context.SaveChangesAsync();
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

            TempData["SuccessMessage"] = "Sharing link created successfully.";
            return RedirectToAction("Index");
        }
        
    }
}
