using System.ComponentModel.DataAnnotations;

namespace AspNetCore3.Models
{
    public class ContactViewModel
    {
        [Required]
        public string Name { get; set; }

        [EmailAddress]
        public string Email { get; set; }
    }
}
