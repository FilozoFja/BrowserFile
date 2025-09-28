using BrowserFile.Models.Entities;

namespace BrowserFile.Models.ViewModels
{
    public class ShareViewCombinedList
    {
        public required StoredFile File { get; set; }
        public required string Link { get; set; }
    }
}