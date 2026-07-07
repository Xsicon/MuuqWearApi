namespace MuuqWear.Model.DTO.CustomerDTO;

public class CustomerNoteDTO
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid AuthorUserId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string? AuthorRole { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateCustomerNoteDTO
{
    public string Body { get; set; } = string.Empty;
}

public class CustomerNoteSummary
{
    public int NoteCount { get; set; }
    public string? LatestNotePreview { get; set; }
    public DateTime? LatestNoteAt { get; set; }
    public string? LatestNoteAuthorName { get; set; }
    public string? LatestNoteAuthorRole { get; set; }
}
