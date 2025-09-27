using BrowserFile.Models.ViewModels;

namespace BrowserFile.Models.Entities
{
    public class PublicFileViewModel
    {
        public string Token { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FileSize { get; set; } = string.Empty;
        public bool IsRequiredPassword { get; set; } = false;
        public bool IsOneTime { get; set; } = false;
    }
}