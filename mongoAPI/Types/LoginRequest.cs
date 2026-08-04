using System.ComponentModel.DataAnnotations;

namespace mongoAPI.Types
{
    public record LoginRequest(
        [Required(ErrorMessage = "Username is required")] string Username,
        [Required(ErrorMessage = "Password is required")] string Password
    );
}
