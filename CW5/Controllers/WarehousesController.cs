using System.Data.SqlTypes;
using CW5.Enums;
using CW5.Models;
using CW5.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CW5.Controllers;

[Route("api/[controller]")]
[ApiController]
public class WarehousesController : ControllerBase
{
    private readonly IWarehouseRepository _warehouseRepository;

    public WarehousesController(IWarehouseRepository warehouseRepository)
    {
        _warehouseRepository = warehouseRepository;
    }

    [HttpPost]
    public async Task<IActionResult> RegisterProduct(ProductRegister productRegister)
    {
        var result = await _warehouseRepository.RegisterProduct(productRegister);

        return result is int
            ? Ok($"Produkt pomyślnie zarejestrowany. Klucz główny produktu w hurtowni: {result}")
            : result switch
            {
                DatabaseStatusEnum.ProductNotPresent => StatusCode(404, "Taki produkt nie istnieje!"),
                DatabaseStatusEnum.InvalidData => BadRequest("Niepoprawne dane!"),
                DatabaseStatusEnum.WarehouseInvalid => StatusCode(404, "Niepoprawny magazyn!"),
                DatabaseStatusEnum.OrderNotPresent => StatusCode(404, "Nie ma odpowiedniego zlecenia!"),
                DatabaseStatusEnum.WrongDate => StatusCode(404, "Niepoprawna data!"),
                DatabaseStatusEnum.OrderAlreadyDone => StatusCode(500, "Zlecenie zostało już zrealizowane!"),
                _ => throw new SqlTypeException()
            };
    }
}