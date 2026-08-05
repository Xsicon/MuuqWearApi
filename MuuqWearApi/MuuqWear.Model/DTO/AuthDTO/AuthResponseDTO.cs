namespace MuuqWear.API.DTO;
public class AuthResponseDTO
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public string? Email { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? Role { get; set; }

    /// <summary>Present on blocked auth responses (suspended / deleted).</summary>
    public string? AccountStatus { get; set; }

    /// <summary>Present when AccountStatus is suspended.</summary>
    public DateTime? SuspendedUntil { get; set; }
}
