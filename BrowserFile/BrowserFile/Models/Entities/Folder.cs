using System.ComponentModel.DataAnnotations.Schema;

namespace BrowserFile.Models.Entities
{
    public class Folder
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string IconId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Tag { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        [ForeignKey("IconId")]
        public virtual Icon Icon { get; set; }
    }
}
