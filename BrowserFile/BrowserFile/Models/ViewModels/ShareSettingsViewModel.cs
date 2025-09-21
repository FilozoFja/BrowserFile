using BrowserFile.Models.Entities;

namespace BrowserFile.Models.ViewModels
{
    public class ShareSettingsViewModel
    {
        public SharedLink? SharedLink { get; set; } = null;
        public StoredFile File { get; set; } = null!;


        public DateTime? ExpirationDate { get; set; } 
        public bool OneTime { get; set; } = false;
        public string? Password { get; set; } = null;
    }
}
