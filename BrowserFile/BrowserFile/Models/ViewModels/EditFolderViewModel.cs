using BrowserFile.Models.DTO;
using BrowserFile.Models.Entities;

namespace BrowserFile.Models.ViewModels
{
    public class EditFolderViewModel
    {
        public string OriginalFolderId { get; set; } 
        public FolderDTO FolderToEdit { get; set; }
        public List<Icon> Icons { get; set; }
    }
}
