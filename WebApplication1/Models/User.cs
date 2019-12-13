using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace DomesticViolenceWebApp.Models
{
    public class User
    {
        public string Id { get; set; }

        [Required(ErrorMessage = "User Id field is required!")]
        public string userId { get; set; }

        [Required(ErrorMessage = "Name field is required!")]
        public string name { get; set; }

        [Required(ErrorMessage = "Age field is required!")]
        public string age { get; set; }

        [Required(ErrorMessage = "Gender field is required!")]
        public string gender { get; set; }
        public string location { get; set; }
        public string emergencyContactOne { get; set; }
        public string emergencyContactTwo { get; set; }
        public string emergencyContactThree { get; set; }
    }
}
