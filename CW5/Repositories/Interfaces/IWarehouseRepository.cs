using CW5.Models;

namespace CW5.Repositories.Interfaces;

public interface IWarehouseRepository
{
    public Task<dynamic> RegisterProduct(ProductRegister productRegister);

    public Task<int> UseTransactionAsync(ProductRegister productRegister);
}