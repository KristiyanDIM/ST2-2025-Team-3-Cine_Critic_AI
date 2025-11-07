using System.ComponentModel.DataAnnotations;

namespace Cine_Critic_AI.Models
{
    public class ResetPasswordViewModel
    {
        [Required]
        public string Token { get; set; }

        [Required, MinLength(6)]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [Compare("NewPassword", ErrorMessage = "Паролите не съвпадат.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }
    }
}
