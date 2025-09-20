namespace BrowserFile.Models.Entities;
public class Icon
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Url { get; set; }

    
    public virtual ICollection<Folder> Folders { get; set; } = new List<Folder>();
}
