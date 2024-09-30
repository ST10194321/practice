using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ABC_MVC.Models
{
    public class ProductViewModel
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be a positive number.")]
        public decimal Price { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        [Required]
        public IFormFile ImageFile { get; set; }
    }
}
