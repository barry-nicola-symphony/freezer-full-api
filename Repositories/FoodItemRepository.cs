using System.Diagnostics;

namespace WebApi.Repositories;

using Dapper;
using System.Collections.Generic;
using WebApi.Entities;
using WebApi.Helpers;

public interface IFoodItemRepository
{
    Task<IEnumerable<FoodItem>> GetAll();
    Task<IEnumerable<FoodItem>> GetById(int id);
    Task<IEnumerable<FoodItem>> GetByName(string search);
    Task Create(FoodItem foodItem);
    Task Update(FoodItem foodItem);
    Task Delete(int id);
}

public class FoodItemRepository(DataContext context) : IFoodItemRepository
{
    public async Task<IEnumerable<FoodItem>> GetAll()
    {
        using var connection = context.CreateConnection();
        var sql = @"
        select fi.FoodItemId, fi.Name, Description, Quantity, FreezerLocation, ItemLocation, DateFrozen, t.TagId, TagName
            from FoodItems fi
            Inner Join FoodItemTags ft on ft.FoodItemId = fi.FoodItemId
            Inner Join Tags t on t.TagId = ft.TagId
            Order By fi.Name
    ";

        var foodItems = await connection.QueryAsync<FoodItem, Tag, FoodItem>(sql, (foodItem, tag) =>
        {
            foodItem.Tags ??= new List<Tag>();
            foodItem.Tags.Add(tag);
            return foodItem;
        }, splitOn: "TagId");

        var result = foodItems.GroupBy(f => f.FoodItemId).Select(g =>
        {
            var groupedFoodItems = g.First();
            groupedFoodItems.Tags = g.Select(selector: f => f.Tags.SingleOrDefault()).ToList();
            return groupedFoodItems;
        });

        return result;
    }


    public async Task<IEnumerable<FoodItem>> GetById(int id)
    {
        using var connection = context.CreateConnection();
        var parameter = new { FoodItemId = id };
        const string sql = @"
        select fi.FoodItemId, fi.Name, Description, Quantity, FreezerLocation, ItemLocation, DateFrozen, t.TagId, TagName
            from FoodItems fi
            Inner Join FoodItemTags ft on ft.FoodItemId = fi.FoodItemId
            Inner Join Tags t on t.TagId = ft.TagId
            Where fi.FoodItemId = @FoodItemId
        ";

        var foodItems = await connection.QueryAsync<FoodItem, Tag, FoodItem>(sql, (foodItem, tag) =>
        {
            foodItem.Tags ??= new List<Tag>();
            foodItem.Tags.Add(tag);
            return foodItem;
        }, parameter, splitOn: "TagId");

        var result = foodItems.GroupBy(f => f.FoodItemId).Select(g =>
        {
            var groupedFoodItems = g.First();
            groupedFoodItems.Tags = g.Select(selector: f => f.Tags.SingleOrDefault()).ToList();
            return groupedFoodItems;
        });

        return result;
    }

    public async Task<IEnumerable<FoodItem>> GetByName(string search)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@Name", "%" + search + "%");
        using var connection = context.CreateConnection();
        var sql = "SELECT * FROM FoodItems WHERE Name LIKE @Name Order By Name";
        var result = await connection.QueryAsync<FoodItem>(sql, parameters);
        return result;
    }

    public async Task Create(FoodItem foodItem)
    {
        using var connection = context.CreateConnection();
        const string sql = @"
           INSERT INTO FoodItems (Name, Description, DateFrozen, Quantity, FreezerLocation, ItemLocation)
           VALUES (@Name, @Description, @DateFrozen, @Quantity, @FreezerLocation, @ItemLocation);
           SELECT last_insert_rowid();
        ";

        var foodItemId = await connection.QuerySingleAsync<int>(sql, foodItem);

        const string tagsSql = @"
        INSERT INTO FoodItemTags (FoodItemId, TagId)
        VALUES (@FoodItemId, @TagId)
    ";

        Debug.Assert(foodItem.Tags != null, "foodItem.Tags != null");
        foreach (var tag in foodItem.Tags)
        {
            await connection.ExecuteAsync(tagsSql, new { FoodItemId = foodItemId, tag.TagId });
        }
    }

    public async Task Update(FoodItem FoodItem)
    {
        using var connection = context.CreateConnection();
        var sql = """
            UPDATE FoodItems 
            SET Name = @Name,
                Description = @Description,
                DateFrozen = @DateFrozen,
                Quantity = @Quantity, 
                FreezerLocation = @FreezerLocation, 
                ItemLocation = @ItemLocation 
            WHERE Id = @Id
        """;
        await connection.ExecuteAsync(sql, FoodItem);
    }

    public async Task Delete(int id)
    {
        using var connection = context.CreateConnection();
        var sql = """
            DELETE FROM FoodItems 
            WHERE Id = @id
        """;
        await connection.ExecuteAsync(sql, new { id });
    }
}