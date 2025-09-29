using AutoMapper;
using BrowserFile.Data;
using BrowserFile.Interface;
using BrowserFile.Models.DTO;
using BrowserFile.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BrowserFile.Services
{
    public class FolderService : IFolderService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FolderService> _logger;
        private readonly IMapper _mapper;
        private readonly IStorageService _storageService;

        public FolderService(ApplicationDbContext context, ILogger<FolderService> logger, 
                            IMapper mapper, IStorageService storageService)
        {
            _context = context;
            _logger = logger;
            _mapper = mapper;
            _storageService = storageService;
        }

        public async Task<List<Folder>> GetUserFoldersAsync(string userId)
        {
            return await _context.Folders
                .Where(f => f.UserId == userId)
                .ToListAsync();
        }

        public async Task<Folder?> GetFolderByIdAsync(string folderId, string userId)
        {
            return await _context.Folders
                .FirstOrDefaultAsync(f => f.Id == folderId && f.UserId == userId);
        }

        public async Task<Folder?> GetFolderWithFilesAsync(string folderId, string userId)
        {
            return await _context.Folders
                .Include(f => f.StoredFiles)
                .FirstOrDefaultAsync(f => f.Id == folderId && f.UserId == userId);
        }

        public async Task<bool> CreateFolderAsync(FolderDTO folderDto, string userId)
        {
            if (folderDto == null || string.IsNullOrWhiteSpace(folderDto.Name))
            {
                _logger.LogWarning("Invalid folder data provided for user {UserId}", userId);
                return false;
            }

            if (!await _storageService.ValidateIconExistsAsync(folderDto.IconId))
            {
                _logger.LogWarning("Invalid icon {IconId} selected for user {UserId}", folderDto.IconId, userId);
                return false;
            }

            try
            {
                var folder = _mapper.Map<Folder>(folderDto);
                folder.Id = Guid.NewGuid().ToString();
                folder.UserId = userId;
                folder.CreatedAt = DateTime.UtcNow;

                await _context.Folders.AddAsync(folder);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Folder created: {FolderName} (ID: {FolderId}) by user {UserId}", 
                    folder.Name, folder.Id, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating folder for user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> UpdateFolderAsync(string folderId, FolderDTO folderDto, string userId)
        {
            if (folderDto == null || string.IsNullOrWhiteSpace(folderDto.Name))
            {
                _logger.LogWarning("Invalid folder data provided for update by user {UserId}", userId);
                return false;
            }

            var folder = await GetFolderByIdAsync(folderId, userId);
            if (folder == null)
            {
                _logger.LogWarning("Folder {FolderId} not found for user {UserId}", folderId, userId);
                return false;
            }

            if (!await _storageService.ValidateIconExistsAsync(folderDto.IconId))
            {
                _logger.LogWarning("Invalid icon {IconId} selected for user {UserId}", folderDto.IconId, userId);
                return false;
            }

            try
            {
                folder.Name = folderDto.Name;
                folder.Description = folderDto.Description;
                folder.Tag = folderDto.Tag;
                folder.IconId = folderDto.IconId;

                _context.Folders.Update(folder);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Folder updated: {FolderName} (ID: {FolderId}) by user {UserId}", 
                    folder.Name, folderId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating folder {FolderId} for user {UserId}", folderId, userId);
                return false;
            }
        }

        public async Task<bool> DeleteFolderAsync(string folderId, string userId)
        {
            if (string.IsNullOrWhiteSpace(folderId))
            {
                _logger.LogWarning("Invalid folder ID provided for deletion by user {UserId}", userId);
                return false;
            }

            var folder = await GetFolderWithFilesAsync(folderId, userId);
            if (folder == null)
            {
                _logger.LogWarning("Folder {FolderId} not found for user {UserId}", folderId, userId);
                return false;
            }

            if (folder.StoredFiles?.Any() == true)
            {
                _logger.LogWarning("Attempted to delete non-empty folder {FolderId} by user {UserId}", folderId, userId);
                return false;
            }

            try
            {
                _context.Folders.Remove(folder);
                await _context.SaveChangesAsync();

                _logger.LogWarning("Folder deleted: {FolderName} (ID: {FolderId}) by user {UserId}", 
                    folder.Name, folderId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting folder {FolderId} for user {UserId}", folderId, userId);
                return false;
            }
        }

        public async Task<bool> FolderHasFilesAsync(string folderId, string userId)
        {
            return await _context.StoredFiles
                .AnyAsync(f => f.FolderId == folderId && f.UserId == userId);
        }

        public async Task<bool> ValidateIconExistsAsync(string iconId)
        {
            return await _storageService.ValidateIconExistsAsync(iconId);
        }

        public async Task<List<Icon>> GetIconsAsync()
        {
            return await _storageService.GetIconsAsync();
        }
    }
}