using System.Data.SqlClient;
using CW5.Enums;
using CW5.Models;
using CW5.Repositories.Interfaces;

namespace CW5.Repositories.Implementations;

public class WarehouseRepository : IWarehouseRepository
{
    private readonly IConfiguration _configuration;

    public WarehouseRepository(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<dynamic> RegisterProduct(ProductRegister productRegister)
    {
        //0. Amount musi być większe niż 0.
        if (productRegister.Amount <= 0) return DatabaseStatusEnum.InvalidData;

        //Otwieramy connection
        await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();

        //Rozpoczynamy transakcje
        await using var transaction = await connection.BeginTransactionAsync();
        int primaryKey = 0;
        try
        {
            //1. Sprawdzamy czy produkt o podanym id istnieje
            var sqlString = "SELECT Price FROM Product WHERE IdProduct = @IdProduct";
            await using var command = new SqlCommand(sqlString, connection);
            command.Parameters.AddWithValue("@IdProduct", productRegister.IdProduct);
            command.Transaction =
                (SqlTransaction)transaction; // Komenda musi mieć transakcje w ramach której działa

            var dataReaderScalar = await command.ExecuteScalarAsync();

            //Jesli nie istnieje - zwroc komunikat
            if (dataReaderScalar == null)
            {
                return DatabaseStatusEnum.ProductNotPresent;
            }

            //Zapisujemy cene
            var price = float.Parse(dataReaderScalar?.ToString()!);


            //2. Sprawdzamy czy hurtownia o podanym id istnieje
            sqlString = "SELECT * FROM Warehouse WHERE IdWarehouse = @IdWarehouse";
            command.Parameters.AddWithValue("@IdWarehouse", productRegister.IdWarehouse);
            command.CommandText = sqlString;

            dataReaderScalar = await command.ExecuteScalarAsync();

            //Jesli nie istnieje - zwroc komunikat
            if (dataReaderScalar == null)
            {
                return DatabaseStatusEnum.WarehouseInvalid;
            }


            //3. Sprawdzamy czy w tabeli Order istnieje rekord z
            //IdProduct i Amount zgodnym z naszym żądaniem
            sqlString = "SELECT CreatedAt, IdOrder FROM [Order] WHERE IdProduct = @IdProduct AND Amount = @Amount";
            command.Parameters.AddWithValue("@Amount", productRegister.Amount);
            command.CommandText = sqlString;

            var dataReader = await command.ExecuteReaderAsync();
            var isOrderPresent = await dataReader.ReadAsync();
            
            //Jesli nie istnieje - zwroc komunikat
            if (!isOrderPresent)
            {
                await dataReader.CloseAsync(); //zamykamy dzialajacy reader
                return DatabaseStatusEnum.OrderNotPresent;
            }
            
            //Zapisz IdOrder
            var idOrder = dataReader["IdOrder"].ToString();


            //4. CreatedAt zamówienia powinno być mniejsze niż CreatedAt pochodzące z naszego żądania
            await dataReader.CloseAsync(); //zamykamy dzialajacy reader
            dataReaderScalar = await command.ExecuteScalarAsync();
            var dateFromOrder = dataReaderScalar!.ToString();

            if (DateTime.Parse(dateFromOrder!) > productRegister.CreatedAt)
            {
                return DatabaseStatusEnum.WrongDate;
            }


            //5. Sprawdzamy czy przypadkiem to zlecenie nie zostało już zrealizowane.
            //Sprawdzamy czy w tabeli Product_Warehouse nie ma już wiersza z danym IdOrder
            sqlString = "SELECT * FROM Product_Warehouse WHERE IdOrder = @IdOrder";
            command.Parameters.AddWithValue("@IdOrder", idOrder);
            command.CommandText = sqlString;

            dataReaderScalar = await command.ExecuteScalarAsync();

            //Jesli zlecenie zostało zrealizowane - zwroc komunikat
            if (dataReaderScalar != null)
            {
                await dataReader.CloseAsync();
                return DatabaseStatusEnum.OrderAlreadyDone;
            }

            //6. Aktualizujemy kolumnę FullfilledAt zlecenia w wierszu oznaczającym zlecenie zgodnie z aktualną datą i czasem.
            sqlString = "UPDATE [Order] SET FulfilledAt = @CreatedAt WHERE IdOrder = @IdOrder";
            command.Parameters.AddWithValue("@CreatedAt", productRegister.CreatedAt);
            command.CommandText = sqlString;

            await command.ExecuteNonQueryAsync();


            //7. Wstawiamy rekord do tabeli Product_Warehouse.
            //Kolumna Price powinna zawierać pomnożoną cenę pojedynczego produktu z wartością Amount z naszego żądania.
            //Ponadto wstawiamy wartość CreatedAt zgodną z aktualnym czasem
            sqlString = "INSERT INTO Product_Warehouse (IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt)" +
                        "VALUES (@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Price * @Amount, @CreatedAt)";
            command.Parameters.AddWithValue("@Price", price);
            command.CommandText = sqlString;

            await command.ExecuteNonQueryAsync();

            //8. Wyciągamy wartość klucza głównego z nowo powstałego rekordu
            sqlString = "SELECT IdProductWarehouse FROM Product_Warehouse WHERE IdOrder = @IdOrder";
            command.CommandText = sqlString;

            dataReaderScalar = await command.ExecuteScalarAsync();
            primaryKey = int.Parse(dataReaderScalar!.ToString()!);

            //Commitujemy na koncu
            await transaction.CommitAsync();
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
        }

        //Jako wynik operacji zwracamy wartość klucza głównego
        //wygenerowanego dla rekordu wstawionego do tabeli Product_Warehouse.
        return primaryKey;
    }


    public async Task<int> UseTransactionAsync(ProductRegister productRegister)
    {
        var idProduct = productRegister.IdProduct;
        var idWarehouse = productRegister.IdWarehouse;
        var amount = productRegister.Amount;
        var createdAt = productRegister.CreatedAt;

        await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();

        await using var command = new SqlCommand();

        command.Connection = connection;
        command.CommandText = "AddProductToWarehouse";
        command.CommandType = System.Data.CommandType.StoredProcedure;

        command.Parameters.AddWithValue("@IdProduct", idProduct);
        command.Parameters.AddWithValue("@IdWarehouse", idWarehouse);
        command.Parameters.AddWithValue("@Amount", amount);
        command.Parameters.AddWithValue("@CreatedAt", createdAt);

        return await command.ExecuteNonQueryAsync();
    }
}