using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace UsersManager.Models
{
    public class ClaimBindingModel
    {
        [Required]
        [Display(Name = "Tipo de sub-rol")] 
        public string Type { get; set; }

        [Required]
        [Display(Name = "Valor del sub-rol")]
        public string Value { get; set; }
    }
}