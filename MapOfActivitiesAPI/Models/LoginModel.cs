using System.ComponentModel.DataAnnotations;


namespace MapOfActivitiesAPI.Models
{
    public class LoginModel
    {
        [Required(ErrorMessage = "The field must not be empty")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "The field must not be empty")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Display(Name = "Remember?")]
        public bool RememberMe { get; set; }


    }
}