using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrowserFile.Models.Entities
{
    public class StoredFile
    {
        [Required]
        public required string Id { get; set; }
        public required string Name { get; set; } 
        public string Size { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string WhoAdded { get; set; } = string.Empty;
        public string FileExtension { get; set; } = string.Empty;
        public required string FilePath { get; set; }
        public bool IsStarred { get; set; } = false;
        public bool IsShared { get; set; } = false;

        public required string UserId { get; set; }
        public required string FolderId { get; set; }
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;
        [ForeignKey("FolderId")]
        public virtual Folder Folder { get; set; } = null!;
        public virtual ICollection<SharedLink>? SharedLink { get; set; } = null;
        
    }
}