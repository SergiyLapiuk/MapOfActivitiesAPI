using System.ComponentModel.DataAnnotations;

namespace MapOfActivitiesAPI.Models
{
    public class ResetPasswordModel
    {
        [Required(ErrorMessage = "Id is required")]
        public string? Id { get; set; }

        [Required(ErrorMessage = "Password is required")]
        public string? Password { get; set; }
        public string Code { get; set; }
    }
}
