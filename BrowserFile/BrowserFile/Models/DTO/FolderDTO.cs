using System.ComponentModel.DataAnnotations;

namespace BrowserFile.Models.DTO
{
    public class FolderDTO
    {
        [Required(ErrorMessage = "Folder name is required.")]
        [StringLength(20, ErrorMessage = "Folder name must not exceed 20 characters.")]
        public string Name { get; set; }

        [StringLength(50, ErrorMessage = "Description must not exceed 50 characters.")]
        public string Description { get; set; }

        [StringLength(4, ErrorMessage = "Tag must not exceed 4 characters.")]
        public string Tag { get; set; }

        [Required(ErrorMessage = "Icon is required.")]
        public string IconId { get; set; }
    }
}
