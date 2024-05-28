using System.Data;

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
    Task Update(FoodItem foodItem, bool foodItemHasChanged, bool tagsHasChanged);
    Task Delete(int id);
}

public class FoodItemRepository(DataContext context) : IFoodItemRepository
{
    public async Task<IEnumerable<FoodItem>> GetAll()
    {
        using var connection = context.CreateConnection();
        const string sql = @"
        select fi.FoodItemId, fi.Name, Description, Quantity, FreezerLocation, ItemLocation, DateFrozen, t.TagId, TagName
            from FoodItems fi
            Left Join FoodItemTags ft on ft.FoodItemId = fi.FoodItemId
            Left Join Tags t on t.TagId = ft.TagId
            Order By fi.Name
    ";

        var foodItems = await connection.QueryAsync<FoodItem, Tag, FoodItem>(sql, (foodItem, tag) =>
        {
            foodItem.Tags ??= [];
            foodItem.Tags.Add(tag);
            return foodItem;
        }, splitOn: "TagId");

        var result = foodItems.GroupBy(f => f.FoodItemId).Select(g =>
        {
            var groupedFoodItems = g.First();
            groupedFoodItems.Tags = g.SelectMany(f => f.Tags ?? Enumerable.Empty<Tag>()).ToList();
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
            Left Join FoodItemTags ft on ft.FoodItemId = fi.FoodItemId
            Left Join Tags t on t.TagId = ft.TagId
            Where fi.FoodItemId = @FoodItemId
        ";

        var foodItems = await connection.QueryAsync<FoodItem, Tag, FoodItem>(sql, (foodItem, tag) =>
        {
            foodItem.Tags ??= [];
            foodItem.Tags.Add(tag);
            return foodItem;
        }, parameter, splitOn: "TagId");

        var result = foodItems.GroupBy(f => f.FoodItemId).Select(g =>
        {
            var groupedFoodItems = g.First();
            groupedFoodItems.Tags = g.SelectMany(f => f.Tags ?? Enumerable.Empty<Tag>()).ToList();
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
        connection.Open();

        using var transaction = connection.BeginTransaction();
        try
        {
            const string sql = """
                               
                                  INSERT INTO FoodItems (Name, Description, DateFrozen, Quantity, FreezerLocation, ItemLocation)
                                  VALUES (@Name, @Description, @DateFrozen, @Quantity, @FreezerLocation, @ItemLocation);
                               """;

            await connection.ExecuteScalarAsync(sql, foodItem, transaction);

            // Get the last inserted book id
            const string latestRowIdSql = "SELECT last_insert_rowid()";
            var foodItemId = await connection.ExecuteScalarAsync<int>(latestRowIdSql, transaction);

            const string tagsSql = """
                                       INSERT INTO FoodItemTags (FoodItemId, TagId)
                                       VALUES (@FoodItemId, @TagId)
                                   """;

            if (foodItem.Tags != null)
                foreach (var tag in foodItem.Tags)
                {
                    await connection.ExecuteAsync(tagsSql, new { FoodItemId = foodItemId, tag.TagId }, transaction);
                }
            transaction.Commit();
        }
        catch (Exception e)
        {
            transaction.Rollback();
            throw new Exception(e.Message);
        }
    }

    public async Task Update(FoodItem foodItem, bool foodItemChanged, bool tagsChanged)
    {
        // if foodItem has changed, update foodItem
        // if foodItem has not changed but tags have, update tags only
        // if both have changed, update both

        using var sqliteConnection = context.CreateConnection();

        switch (foodItemChanged)
        {
            case true:
                {
                    sqliteConnection.Open();
                    using var transaction = sqliteConnection.BeginTransaction();
                    try
                    {
                        var sqlUpdateFoodItem = SqlUpdateFoodItem();
                        await sqliteConnection.ExecuteAsync(sqlUpdateFoodItem, foodItem, transaction);

                        if (tagsChanged)
                        {
                            var sqlDeleteTags = SqlDeleteTags();
                            await sqliteConnection.ExecuteAsync(sqlDeleteTags, foodItem, transaction);
                            await InsertTags(foodItem, sqliteConnection, transaction);
                        }
                        transaction.Commit();
                    }
                    catch (Exception e)
                    {
                        transaction.Rollback();
                        throw new Exception(e.Message);
                    }
                    break;
                }
            case false when tagsChanged:
                {
                    sqliteConnection.Open();
                    using var tagTransaction = sqliteConnection.BeginTransaction();
                    try
                    {
                        var sqlDeleteTags = SqlDeleteTags();
                        await sqliteConnection.ExecuteAsync(sqlDeleteTags, foodItem, tagTransaction);
                        await InsertTags(foodItem, sqliteConnection, tagTransaction);
                        tagTransaction.Commit();
                    }
                    catch (Exception e)
                    {
                        tagTransaction.Rollback();
                        throw new Exception(e.Message);
                    }
                    break;
                }
        }
    }

    private static async Task InsertTags(FoodItem foodItem, IDbConnection sqliteConnection, IDbTransaction transaction)
    {
        if (foodItem.Tags != null)
        {
            foreach (var tag in foodItem.Tags)
            {
                var sqlInsertTags = SqlInsertTags();
                await sqliteConnection.ExecuteAsync(sqlInsertTags, new { foodItem.FoodItemId, tag.TagId }, transaction);
            }
        }
    }

    private static string SqlInsertTags()
    {
        var sqlInsertTags = """
                            INSERT INTO FoodItemTags (FoodItemId, TagId)
                            VALUES (@FoodItemId, @TagId)
                            """;
        return sqlInsertTags;
    }

    private static string SqlDeleteTags()
    {
        var sqlDeleteTags = """
                              DELETE FROM FoodItemTags
                              WHERE FoodItemId = @FoodItemId
                            """;
        return sqlDeleteTags;
    }

    private static string SqlUpdateFoodItem()
    {
        const string sqlUpdateFoodItem = """

                                         UPDATE FoodItems
                                         SET Name = @Name,
                                             Description = @Description,
                                             DateFrozen = @DateFrozen,
                                             Quantity = @Quantity,
                                             FreezerLocation = @FreezerLocation,
                                             ItemLocation = @ItemLocation
                                         WHERE FoodItemId = @FoodItemId
                                         """;
        return sqlUpdateFoodItem;
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