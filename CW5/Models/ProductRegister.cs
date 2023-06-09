using System.ComponentModel.DataAnnotations;

namespace CW5.Models;

public class ProductRegister
{
    [Required] public int IdProduct { get; set; }
    [Required] public int IdWarehouse { get; set; }
    [Required] public int Amount { get; set; }
    [Required] public DateTime CreatedAt { get; set; }
}