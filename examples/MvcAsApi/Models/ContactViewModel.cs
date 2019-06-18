using System.ComponentModel.DataAnnotations;

namespace MvcAsApi.Models
{
    public class ContactViewModel
    {
        [Required]
        public string Name { get; set; }

        [EmailAddress]
        public string Email { get; set; }
    }
}
