using BrowserFile.Data;
using BrowserFile.Interface;
using BrowserFile.Models.Entities;
using BrowserFile.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1.Ocsp;

namespace BrowserFile.Service
{
    public class FileShareService : IFileShareService
    {
        private readonly ILogger<FileShareService> _logger;
        private readonly ApplicationDbContext _context;
        
        public FileShareService(ILogger<FileShareService> logger, 
                    ApplicationDbContext context)
        {
            _context = context;
            _logger = logger;
        }
        
        public List<ShareViewCombinedList> GetCombinedList(List<StoredFile> sharedFiles, 
                                                            List<SharedLink> sharedLinks,
                                                            string baseUrl)
        {
            List<ShareViewCombinedList> combinedlist = new List<ShareViewCombinedList>();
            foreach (var sharedLink in sharedLinks)
            {
                foreach (var sharedFile in sharedFiles)
                {
                    if (sharedFile.Id == sharedLink.FileId)
                    {
                        combinedlist.Add(new ShareViewCombinedList
                        {
                            File = sharedFile,
                            Link = $"{baseUrl}/share/{sharedLink.Token}"
                        });
                    }
                }
            }

            return combinedlist;
        }

        public async Task<SharedLink?> GetSharedLink(string currentUser, string fileId)
        {
            return await _context.SharedLinks
                .Include(f => f.File)
                .Where(x => x.File != null
                            && x.File.Id == fileId
                            && x.File.UserId == currentUser
                            && x.ExpiresAt > DateTime.Now
                            && ((x.OneTime == true 
                                 && x.Used < 1) || (x.OneTime == false)))
                .FirstOrDefaultAsync();
        }

        public async Task<List<SharedLink>?> GetSharedLinks(string currentUser)
        {
            return await _context.SharedLinks
                .Include(f => f.File)
                .Where(x => x.File != null
                            && x.File.UserId == currentUser
                            && x.ExpiresAt > DateTime.Now.AddSeconds(1)
                            && ((x.OneTime == true 
                                 && x.Used < 1) || (x.OneTime == false)))
                .ToListAsync();
        }

        public async Task<List<StoredFile>?> GetSharedFiles(string currentUser)
        {
            return await _context.StoredFiles
                .Include(f => f.SharedLink)
                .Where(x => x.UserId == currentUser 
                            && x.IsShared 
                            && x.SharedLink != null 
                            && x.SharedLink.Any(xs => xs.ExpiresAt > DateTime.Now)
                            && x.SharedLink.Any(xs => (xs.OneTime && xs.Used <1 )
                                                      || xs.OneTime == false))
                .ToListAsync();
        }

        public async Task<StoredFile?> GetSharedFile(string currentUser, string fileId)
        {
            return await _context.StoredFiles.FirstOrDefaultAsync(x => x.Id == fileId && x.UserId == currentUser);
        }

        public async Task DeactivateSharedLink(string currentUser, string fileId)
        {
            var sharedFile = await GetSharedLink(currentUser, fileId);
            if (sharedFile == null)
            {
                throw new InvalidOperationException("Shared link not found.");
            }
            try
            {
                sharedFile.ExpiresAt = DateTime.Now;
                _context.SharedLinks.Update(sharedFile);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new DbUpdateException($"Something went wrong with our database. " +
                                            $"Please try again later or contact system administrator.");
            }
        }

        public async Task<List<SharedLink?>> GetSharingHistory(string currentUser, string fileId)
        {
            return await _context.SharedLinks
                .Include(f => f.File)
                .Where(x => x.FileId == fileId 
                            && x.File.UserId == currentUser)
                .OrderByDescending(x => x.ExpiresAt)
                .Take(10) 
                .ToListAsync(); 
        }
    }
}