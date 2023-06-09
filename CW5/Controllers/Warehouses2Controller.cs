using CW5.Models;
using CW5.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CW5.Controllers;

[Route("api/[controller]")]
[ApiController]
public class Warehouses2Controller : ControllerBase
{
    private readonly IWarehouseRepository _warehouseRepository;

    public Warehouses2Controller(IWarehouseRepository warehouseRepository)
    {
        _warehouseRepository = warehouseRepository;
    }

    [HttpPost]
    public async Task<IActionResult> UseTransactionAsync(ProductRegister productRegister)
    {
        var result = await _warehouseRepository.UseTransactionAsync(productRegister);

        return result > 0 ? Ok("Procedura poprawnie wykonana.") : StatusCode(500, "Błąd - procedura nie została wykonana");
    }
}