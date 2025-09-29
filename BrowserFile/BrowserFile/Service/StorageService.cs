using BrowserFile.Data;
using BrowserFile.Interface;
using BrowserFile.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BrowserFile.Service
{
    public class StorageService : IStorageService
    {
        private const string ICONS_CACHE_KEY = "icons";
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<StorageService> _logger;

        public StorageService(ApplicationDbContext context, IMemoryCache cache, ILogger<StorageService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<List<Icon>> GetIconsAsync()
        {
            if (!_cache.TryGetValue(ICONS_CACHE_KEY, out List<Icon>? icons) || icons == null)
            {
                icons = await _context.Icons.ToListAsync();
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromHours(1));
                _cache.Set(ICONS_CACHE_KEY, icons, cacheEntryOptions);

                _logger.LogDebug("Icons loaded and cached");
            }
            return icons;
        }

        public async Task<bool> ValidateIconExistsAsync(string iconId)
        {
            var icons = await GetIconsAsync();
            return icons.Any(i => i.Id == iconId);
        }

        public async Task<bool> UserHasPermissionToFolderAsync(string folderId, string userId)
        {
            return await _context.Folders.AnyAsync(f => f.Id == folderId && f.UserId == userId);
        }

        public async Task<Folder?> GetFolderWithDetailsAsync(string folderId, string userId)
        {
            return await _context.Folders
                .Include(f => f.StoredFiles)
                .Include(f => f.Icon)
                .FirstOrDefaultAsync(f => f.Id == folderId && f.UserId == userId);
        }

        public async Task<int> GetFolderFileCountAsync(string folderId, string userId)
        {
            return await _context.StoredFiles
                .CountAsync(f => f.FolderId == folderId && f.UserId == userId);
        }

        public async Task<long> GetFolderSizeAsync(string folderId, string userId)
        {
            var files = await _context.StoredFiles
                .Where(f => f.FolderId == folderId && f.UserId == userId)
                .ToListAsync();
            
            long totalBytes = 0;
            foreach (var file in files)
            {
                if (!string.IsNullOrEmpty(file.Size))
                {
                    var sizeStr = file.Size.Replace(" KB", "").Trim();
                    if (double.TryParse(sizeStr, out double sizeKb))
                    {
                        totalBytes += (long)(sizeKb * 1024);
                    }
                }
            }

            return totalBytes;
        }
    }
}