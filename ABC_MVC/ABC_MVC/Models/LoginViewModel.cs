using System.ComponentModel.DataAnnotations;

namespace ABC_MVC.Models
{
    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } //user's email address

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } //User's password for email address
    }

}
