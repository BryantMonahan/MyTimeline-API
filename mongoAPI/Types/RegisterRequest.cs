using System.ComponentModel.DataAnnotations;

namespace mongoAPI.Types
{
    public record RegisterRequest(
        [Required(ErrorMessage = "Username is required")] string Username,
        [Required(ErrorMessage = "Password is required")] string Password,
        [Required(ErrorMessage = "Email is required")] string Email
        );
}
