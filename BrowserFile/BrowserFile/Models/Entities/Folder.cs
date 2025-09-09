using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrowserFile.Models.Entities
{
    public class Folder
    {
        [Required]
        public required string Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string IconId { get; set; } = string.Empty;  
        public required string Name { get; set; } = string.Empty;
        public string? Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? Tag { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        [ForeignKey("IconId")]
        public virtual Icon Icon { get; set; }
    }
}
