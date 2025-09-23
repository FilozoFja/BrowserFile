using BrowserFile.Models.Entities;

namespace BrowserFile.Models.ViewModels
{
    public class ShareViewModel
    {
        public List<StoredFile> SharedFiles { get; set; } = new List<StoredFile>();
    }
}
