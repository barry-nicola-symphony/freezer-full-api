namespace WebApi.Helpers;

using Dapper;
using Microsoft.Data.Sqlite;
using System.Data;

public class DataContext
{
    protected readonly IConfiguration Configuration;

    public DataContext(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IDbConnection CreateConnection()
    {
        return new SqliteConnection(Configuration.GetConnectionString("WebApiDatabase"));
    }

    public async Task Init()
    {
        // create database tables if they don't exist
        using var connection = CreateConnection();
        await _initFoodItems();

        async Task _initFoodItems()
        {
            var sql = """
                CREATE TABLE IF NOT EXISTS 
                FoodItems (
                    FoodItemId INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    Name TEXT COLLATE NOCASE,
                    Description TEXT COLLATE NOCASE,
                    DateFrozen TEXT,
                    Quantity REAL,
                    FreezerLocation TEXT,
                    ItemLocation TEXT
                );
            """;
            await connection.ExecuteAsync(sql);
        }
    }
}