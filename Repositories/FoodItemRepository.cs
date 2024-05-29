using System.Data;

namespace WebApi.Repositories;

using Dapper;
using System.Collections.Generic;
using WebApi.Entities;
using WebApi.Helpers;

public interface IFoodItemRepository
{
    Task<IEnumerable<FoodItem>> GetAll();
    Task<FoodItem> GetFoodItemById(int id);
    Task<IEnumerable<FoodItem>> GetFoodItemAndTagsById(int id);
    Task<IEnumerable<FoodItem>> GetByName(string search);
    Task CreateFoodItem(FoodItem foodItem);
    Task UpdateFoodItem(FoodItem FoodItem);
    Task UpdateFoodItemAndTags(FoodItem foodItem, bool foodItemHasChanged, bool tagsHasChanged);
    Task DeleteFoodItem(int id);
    Task<List<Tag>> GetAllTags();
    Task<int>? CreateTag(string tagName);
    Task UpdateTag(int id, string tagName);
    Task DeleteTag(int id);
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


    public async Task<FoodItem> GetFoodItemById(int id)
    {
        using var connection = context.CreateConnection();
        var sql = """
                      SELECT * FROM FoodItems
                      WHERE FoodItemId = @id
                  """;
        var result = await connection.QuerySingleOrDefaultAsync<FoodItem>(sql, new { id });
        return result ?? new FoodItem();
    }

    public async Task<IEnumerable<FoodItem>> GetFoodItemAndTagsById(int id)
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

    public async Task CreateFoodItem(FoodItem foodItem)
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

    public async Task UpdateFoodItem(FoodItem FoodItem)
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
                      WHERE FoodItemId = @FoodItemId
                  """;
        await connection.ExecuteAsync(sql, FoodItem);
    }
    public async Task UpdateFoodItemAndTags(FoodItem foodItem, bool foodItemChanged, bool tagsChanged)
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
                            var sqlDeleteTags = SqlDeleteFoodItemTags();
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
                        var sqlDeleteTags = SqlDeleteFoodItemTags();
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
                var sqlInsertTags = SqlInsertFoodItemTags();
                await sqliteConnection.ExecuteAsync(sqlInsertTags, new { foodItem.FoodItemId, tag.TagId }, transaction);
            }
        }
    }

    private static string SqlInsertFoodItemTags()
    {
        var sqlInsertTags = """
                            INSERT INTO FoodItemTags (FoodItemId, TagId)
                            VALUES (@FoodItemId, @TagId)
                            """;
        return sqlInsertTags;
    }

    private static string SqlDeleteFoodItemTags()
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

    public async Task DeleteFoodItem(int id)
    {
        using var connection = context.CreateConnection();

        var sql = "DELETE FROM FoodItemTags Where FoodItemId = @id";
        await connection.ExecuteAsync(sql, new { id });

        sql = "DELETE FROM FoodItems WHERE FoodItemId = @id";
        await connection.ExecuteAsync(sql, new { id });

    }

    public async Task<List<Tag>> GetAllTags()
    {
        using var connection = context.CreateConnection();
        const string sql = "Select TagId, TagName FROM Tags ORDER BY TagName";
        var result = await connection.QueryAsync<Tag>(sql);
        return result.ToList();
    }

    public async Task<int>? CreateTag(string tagName)
    {
        using var connection = context.CreateConnection();
        var parameter = new { TagName = tagName };
        connection.Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            var sql = "INSERT INTO Tags (TagName) VALUES (@TagName)";
            await connection.ExecuteAsync(sql, parameter, transaction);

            // Get the last inserted tag id
            sql = "SELECT last_insert_rowid()";
            var result = await connection.ExecuteScalarAsync<int>(sql, transaction);
            transaction.Commit();

            return result;

        }
        catch (Exception e)
        {
            transaction.Rollback();
            throw new Exception(e.Message);
        }
    }

    public async Task UpdateTag(int id, string tagName)
    {
        using var connection = context.CreateConnection();
        var sql = """
                      UPDATE Tags SET TagName = @tagName
                      WHERE TagId = @id
                  """;
        await connection.ExecuteAsync(sql, new { id, tagName });
    }

    public async Task DeleteTag(int id)
    {
        using var connection = context.CreateConnection();
        var sql = """
                      DELETE FROM Tags
                      WHERE TagId = @id
                  """;
        await connection.ExecuteAsync(sql, new { id });
    }
}