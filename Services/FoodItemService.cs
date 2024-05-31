using System.Data;
using WebApi.Helpers;

namespace WebApi.Services;

using WebApi.Entities;
using WebApi.Models.FoodItems;
using WebApi.Repositories;

public interface IFoodItemService
{
    Task<IEnumerable<FoodItem>> GetAllFoodItems();
    Task<FoodItem> GetFoodItemById(int id);
    Task<IEnumerable<FoodItem>> GetFoodItemByName(string name);
    Task CreateFoodItem(CreateRequest model);
    Task UpdateFoodItem(int id, UpdateRequest model);
    Task UpdateFoodItemAndTags(int id, UpdateRequest model);
    Task DeleteFoodItem(int id);
    Task<List<Tag>> GetAllTags();
    Task<List<Tag>> GetSelectedTagsForFoodItem(int id);
    Task<int> CreateTag(string tagName);
    Task UpdateTag(int id, string tagName);
    Task DeleteTag(int id);
}

public class FoodItemService(IFoodItemRepository foodItemRepository) : IFoodItemService
{
    public async Task<IEnumerable<FoodItem>> GetAllFoodItems()
    {
        return await foodItemRepository.GetAll();
    }

    public async Task<FoodItem> GetFoodItemById(int id)
    {
        var foodItem = await foodItemRepository.GetFoodItemById(id);

        return foodItem ?? throw new KeyNotFoundException("FoodItem not found");
    }
    public async Task<IEnumerable<FoodItem>> GetFoodItemAndTagsById(int id)
    {
        var foodItem = await foodItemRepository.GetFoodItemAndTagsById(id) ?? throw new KeyNotFoundException("FoodItem not found");
        return foodItem;
    }

    public async Task<IEnumerable<FoodItem>> GetFoodItemByName(string name)
    {
        return await foodItemRepository.GetByName(name);
    }

    public async Task CreateFoodItem(CreateRequest model)
    {
        // map model to new FoodItem object
        var foodItem = new FoodItem
        {
            Name = model.Name,
            Description = model.Description,
            DateFrozen = model.DateFrozen,
            Quantity = (int)model.Quantity!,
            FreezerLocation = model.FreezerLocation,
            ItemLocation = model.ItemLocation,
            Tags = []
        };

        if (model.Tags?.Count == 0 || model.Tags == null)
        {
            model.Tags = [new Tag { TagId = 222, TagName = "uncategorised" }];
        }

        foodItem.Tags = model.Tags?.Select(t => new Tag { TagId = t.TagId, TagName = t.TagName }).ToList();

        if (string.IsNullOrEmpty(foodItem.DateFrozen)) foodItem.DateFrozen = DateTime.Now.ToString("yyyy-MM-dd");

        // save FoodItem
        await foodItemRepository.CreateFoodItem(foodItem);
    }

    public async Task UpdateFoodItem(int id, UpdateRequest model)
    {
        var foodItem = await foodItemRepository.GetFoodItemById(id);

        if (foodItem == null)
            throw new KeyNotFoundException("FoodItem not found");

        //convert model to FoodItem object
        var foodItemToUpdate = new FoodItem
        {
            FoodItemId = id,
            Name = model.Name,
            Description = model.Description,
            DateFrozen = model.DateFrozen,
            Quantity = model.Quantity ?? 0,
            FreezerLocation = model.FreezerLocation,
            ItemLocation = model.ItemLocation,
            Tags = model.Tags?.Select(t => new Tag { TagId = t.TagId, TagName = t.TagName }).ToList() ?? new List<Tag>()
        };

        // save FoodItem
        await foodItemRepository.UpdateFoodItemAndTags(foodItemToUpdate);
    }

    public async Task UpdateFoodItemAndTags(int id, UpdateRequest model)
    {
        var dbFoodItems = await foodItemRepository.GetFoodItemAndTagsById(id);
        var foodItem = (dbFoodItems.FirstOrDefault()) ?? throw new KeyNotFoundException("FoodItem not found");

        var foodItemHasChanged = FoodItemHasChanged(ref model, ref foodItem);
        var tagsChanged = FoodItemTagHasChanged(ref model, ref foodItem);

        if (!foodItemHasChanged && !tagsChanged) return;

        //convert model to FoodItem object
        var foodItemToUpdate = new FoodItem
        {
            FoodItemId = id,
            Name = model.Name,
            Description = model.Description,
            DateFrozen = model.DateFrozen,
            Quantity = model.Quantity ?? 0,
            FreezerLocation = model.FreezerLocation,
            ItemLocation = model.ItemLocation,
            Tags = model.Tags?.Select(t => new Tag { TagId = t.TagId, TagName = t.TagName }).ToList() ?? new List<Tag>()
        };

        await foodItemRepository.UpdateFoodItemAndTags(foodItemToUpdate, foodItemHasChanged, tagsChanged);
    }

    private static bool FoodItemHasChanged(ref UpdateRequest model, ref FoodItem foodItem)
    {
        if (model.Name != null && !model.Name.Equals(foodItem.Name)) return true;
        if (model.Description != null && !model.Description.Equals(foodItem.Description)) return true;
        if (model.DateFrozen != null && !model.DateFrozen.Equals(foodItem.DateFrozen)) return true;
        if (model.Quantity.HasValue && !model.Quantity.Equals(foodItem.Quantity)) return true;
        if (model.FreezerLocation != null && !model.FreezerLocation.Equals(foodItem.FreezerLocation)) return true;
        return model.ItemLocation != null && !model.ItemLocation.Equals(foodItem.ItemLocation);
    }

    public static bool FoodItemTagHasChanged(ref UpdateRequest model, ref FoodItem foodItem)
    {
        var tagsChanged = model.Tags is { Count: > 0 } && foodItem.Tags is { Count: > 0 } && !model.Tags.SequenceEqual(foodItem.Tags, new TagComparer());
        return tagsChanged;
    }

    public async Task DeleteFoodItem(int id)
    {
        await foodItemRepository.DeleteFoodItem(id);
    }

    public async Task<List<Tag>> GetAllTags()
    {
        var result = await foodItemRepository.GetAllTags();
        return result;
    }

    public async Task<List<Tag>> GetSelectedTagsForFoodItem(int id)
    {
        var result = await foodItemRepository.GetSelectedTagsForFoodItem(id);
        return result;
    }

    public async Task<int> CreateTag(string tagName)
    {
        var result = await foodItemRepository.CreateTag(tagName);
        return result;
    }

    public async Task UpdateTag(int id, string tagName)
    {
        await foodItemRepository.UpdateTag(id, tagName);
    }

    public async Task DeleteTag(int id)
    {
        await foodItemRepository.DeleteTag(id);
    }
}