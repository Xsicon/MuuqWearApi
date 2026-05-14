namespace MuuqWear.Model.DTO.ProfileDTO;

public class ProfileDTO
{
    public Guid Id { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool? IsDeleted { get; set; }
    public DateTime? NotificationsReadAt { get; set; }

}

public class UpdateProfileDTO
{
    public string FullName { get; set; } = string.Empty;
}

