using System.ComponentModel.DataAnnotations;

namespace Cine_Critic_AI.Models
{
    public class Movie
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Заглавие")]
        public string Title { get; set; }

        [Required]
        [Display(Name = "Година")]
        public int Year { get; set; }

        [Required]
        [Display(Name = "Жанр")]
        public string Genre { get; set; }

        [Required]
        [Display(Name = "Режисьор")]
        public string Director { get; set; }

        [Display(Name = "Описание (по избор)")]
        public string? Description { get; set; } // вече не е задължително

        [Display(Name = "Изображение (URL или локален път)")]
        public string? ImageUrl { get; set; }
        [Display(Name = "Дата на добавяне")]
        public DateTime AddedOn { get; set; } = DateTime.Now; //
    }
}

