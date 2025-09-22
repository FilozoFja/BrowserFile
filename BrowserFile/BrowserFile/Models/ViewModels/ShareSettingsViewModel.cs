using BrowserFile.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace BrowserFile.Models.ViewModels
{
    public class ShareSettingsViewModel
    {
        public SharedLink? SharedLink { get; set; } = null;
        public StoredFile? File { get; set; } = null;
        public List<SharedLink> SharingHistory { get; set; } = new(); 

        [Display(Name = "Expiration Date")]
        public DateTime? ExpirationDate { get; set; }

        [Display(Name = "One-time download")]
        public bool OneTime { get; set; } = false;

        [Display(Name = "Password")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters")]
        public string? Password { get; set; } = null;

        public string? ShareUrl { get; set; } = null;

        public bool HasActiveLink => SharedLink?.IsValid == true;
        public string FileName => File?.Name ?? "Unknown";
    }
}