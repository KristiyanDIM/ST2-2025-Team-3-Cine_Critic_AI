using System.ComponentModel.DataAnnotations;

namespace Cine_Critic_AI.Models
{
    public class DeleteAccountViewModel
    {
        [Required(ErrorMessage = "Имейлът е задължителен.")]
        [EmailAddress(ErrorMessage = "Невалиден имейл.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Паролата е задължителна.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
