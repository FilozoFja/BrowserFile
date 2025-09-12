namespace BrowserFile.Models.DTO
{
    public class StoredFileDTO
    {
        [Required(ErrorMessage = "File name is required.")]
        [StringLength(50, ErrorMessage = "File name must not exceed 50 characters.")]
        public required string Name { get; set; }
        public bool IsStarred { get; set; } = false; 

    }   
}