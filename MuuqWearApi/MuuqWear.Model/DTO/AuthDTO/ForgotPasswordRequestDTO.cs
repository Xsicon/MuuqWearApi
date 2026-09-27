namespace MuuqWear.API.DTO;

public class ForgotPasswordRequestDTO
{
    /// <summary>Email to send the reset link to.</summary>
    public string? Email { get; set; }

    /// <summary>
    /// Optional frontend reset URL. Must be an allow-listed origin ending in /auth/reset-password.
    /// </summary>
    public string? RedirectTo { get; set; }
}