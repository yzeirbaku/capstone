using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace DomesticViolenceWebApp.Models
{
    public class Admin
    {
        public string Id { get; set; }

        [Display(Name = "Mail")]
        [Required(ErrorMessage = "This field is required")]
        public string Mail { get; set; }

        [Display(Name = "Password")]
        [DataType(DataType.Password)]
        [Required(ErrorMessage = "This field is required")]
        public string Password { get; set; }

        public string loginError;
    }
}
