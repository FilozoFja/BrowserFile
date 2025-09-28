using BrowserFile.Models.Entities;
using BrowserFile.Models.ViewModels;

namespace BrowserFile.Interface
{
    public interface IFileShareService
    {
        public List<ShareViewCombinedList> GetCombinedList(List<StoredFile> files, 
                                                            List<SharedLink> sharedLinks, 
                                                            string baseUrl);
        
        public Task<SharedLink?> GetSharedLink(string currentUser, string fileId);
        public Task<List<SharedLink>?> GetSharedLinks(string currentUser);
        public Task<List<StoredFile>?> GetSharedFiles(string currentUser);
        public Task<StoredFile?> GetSharedFile(string currentUser, string fileId);
        public Task DeactivateSharedLink(string currentUser, string fileId);
        public Task<List<SharedLink?>> GetSharingHistory(string currentUser, string fileId);
    }
}