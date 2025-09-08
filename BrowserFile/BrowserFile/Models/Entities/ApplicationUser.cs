using Microsoft.AspNetCore.Identity;

namespace BrowserFile.Models.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public virtual ICollection<Folder> Folders { get; set; } = new List<Folder>();
    }
}
