using System.ComponentModel.DataAnnotations;

namespace Cine_Critic_AI.Models
{
    public class ForgotPasswordViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; }
    }
}
