using System.ComponentModel.DataAnnotations;

namespace MapOfActivitiesAPI.Models
{
    public class ForgotPasswordModel
    {
        [Required(ErrorMessage = "Email is required")]
        public string? Email { get; set; }
    }
}
