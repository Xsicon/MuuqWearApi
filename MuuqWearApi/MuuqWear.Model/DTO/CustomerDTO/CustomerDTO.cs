namespace MuuqWear.Model.DTO.CustomerDTO;


public class CustomerDTO
{
    public Guid Id { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public DateTime? CreatedAt { get; set; }
    public long? OrderCount { get; set; }
    public decimal TotalSpent { get; set; }
    public DateTime? LastOrderAt { get; set; }
    public int NoteCount { get; set; }
    public string? LatestNotePreview { get; set; }
    public DateTime? LatestNoteAt { get; set; }
    public string? LatestNoteAuthorName { get; set; }
    public string? LatestNoteAuthorRole { get; set; }
}
