using BrowserFile.Models.Entities;

namespace BrowserFile.Models.ViewModels
{
    public class EditFolderViewModel
    {
        public Folder OriginalFolder { get; set; } 
        public Folder FolderToCreate { get; set; }
        public List<Icon> Icons { get; set; }
    }
}
