using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrowserFile.Models.Entities
{
    public class SharedLink
    {
        [Required]
        public required string Id { get; set; }
        [Required]
        public required string FileId { get; set; }
        [Required]
        public required string Token { get; set; }
        public string? Alias { get; set; } = null;
        public DateTime ExpiresAt { get; set; }
        public string? PasswordHash { get; set; }
        public bool OneTime { get; set; } = false;
        public int Used { get; set; } = 0;

        [ForeignKey(nameof(FileId))]
        public virtual StoredFile? File { get; set; }
        [NotMapped]
        public bool IsExpired => DateTime.Now > ExpiresAt;
        [NotMapped]
        public bool IsUsedUp => OneTime && Used > 0;
        [NotMapped]
        public bool IsValid => !IsExpired && !IsUsedUp;
        [NotMapped]
        public bool HasPassword => !string.IsNullOrEmpty(PasswordHash);
    }
}
