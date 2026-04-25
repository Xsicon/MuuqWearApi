namespace MuuqWear.API.DTO;

public class ResetPasswordRequestDTO
{
    // token from reset email URL fragment
    public string? AccessToken { get; set; }

    // refresh token from URL fragment
    public string? RefreshToken { get; set; }

    // new password user wants to set
    public string? NewPassword { get; set; }

    // confirmation — must match NewPassword
    public string? ConfirmPassword { get; set; }
}