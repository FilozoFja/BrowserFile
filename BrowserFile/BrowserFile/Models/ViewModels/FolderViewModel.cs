using BrowserFile.Models.DTO;
using BrowserFile.Models.Entities;

namespace BrowserFile.Models.ViewModels
{
    public class FolderViewModel
    {
        public List<Folder> Folders { get; set; }
        public List<Icon> Icons { get; set; }
        public FolderDTO FolderToCreate { get; set; }
        public string FolderToDeleteId { get; set; }
    }
}
