using BrowserFile.Models.ViewModels;

namespace BrowserFile.Models.ViewModels
{
    public class PublicFileViewModel
    {
        public string Token { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FileSize { get; set; } = string.Empty;
        public bool IsRequiredPassword { get; set; } = false;
        public bool IsOneTime { get; set; } = false;
        public string FileExtension  { get; set; } = string.Empty;
    }
}