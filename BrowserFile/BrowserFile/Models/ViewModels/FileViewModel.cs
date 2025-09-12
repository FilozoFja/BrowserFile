using BrowserFile.Models.Entities;

namespace BrowserFile.Models.ViewModels
{
    public class FileViewModel
    {
        public List<StoredFile> Files { get; set; } = new List<StoredFile>();
        public string CurrentFolderId { get; set; } = string.Empty;
        public string FolderName { get; set; } = string.Empty;
    }
}