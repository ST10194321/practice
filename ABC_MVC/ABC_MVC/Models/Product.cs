using System.ComponentModel.DataAnnotations;

namespace ABC_MVC.Models
{
    public class Product
    {
        public int Id { get; set; }  // Unique identifier for the product

        [Required]
        [StringLength(100)]
        public string Name { get; set; }  // The name of the product

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be a positive number.")]
        public decimal Price { get; set; } // The price of the product

        [StringLength(1000)]
        public string Description { get; set; } //Description of the product

        [DataType(DataType.Url)]
        public string ImageUrl { get; set; } //Product url for the image
    }
}
