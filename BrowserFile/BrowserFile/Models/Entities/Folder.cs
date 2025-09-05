namespace BrowserFile.Models.Entities
{
    public class Folder
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string Name { get; set; }
        public string IconUrl { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Tag { get; set; }
    }
}
