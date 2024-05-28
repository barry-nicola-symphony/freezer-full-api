using System.Data;
using WebApi.Helpers;

namespace WebApi.Services;

using WebApi.Entities;
using WebApi.Models.FoodItems;
using WebApi.Repositories;

public interface IFoodItemService
{
    Task<IEnumerable<FoodItem>> GetAll();
    Task<IEnumerable<FoodItem>> GetById(int id);
    Task<IEnumerable<FoodItem>> GetByName(string name);
    Task Create(CreateRequest model);
    Task Update(int id, UpdateRequest model);
    Task Delete(int id);
}

public class FoodItemService : IFoodItemService
{
    private readonly IFoodItemRepository _FoodItemRepository;

    public FoodItemService(
        IFoodItemRepository FoodItemRepository)
    {
        _FoodItemRepository = FoodItemRepository;
    }

    public async Task<IEnumerable<FoodItem>> GetAll()
    {
        return await _FoodItemRepository.GetAll();
    }

    public async Task<IEnumerable<FoodItem>> GetById(int id)
    {
        var foodItem = await _FoodItemRepository.GetById(id);

        if (foodItem == null)
            throw new KeyNotFoundException("FoodItem not found");

        return foodItem;
    }

    public async Task<IEnumerable<FoodItem>> GetByName(string name)
    {
        return await _FoodItemRepository.GetByName(name);
    }

    public async Task Create(CreateRequest model)
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

        if (model.Tags == null || model.Tags.Count == 0)
        {
            model.Tags = [new Tag { TagId = 222, TagName = "uncategorised" }];
        }

        foodItem.Tags = model.Tags.Select(t => new Tag { TagId = t.TagId, TagName = t.TagName }).ToList();

        if (string.IsNullOrEmpty(foodItem.DateFrozen)) foodItem.DateFrozen = DateTime.Now.ToString("yyyy-MM-dd");

        // save FoodItem
        await _FoodItemRepository.Create(foodItem);
    }

    public async Task Update(int id, UpdateRequest model)
    {
        var dbFoodItems = await _FoodItemRepository.GetById(id);
        var foodItem = (dbFoodItems?.FirstOrDefault()) ?? throw new KeyNotFoundException("FoodItem not found");

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

        await _FoodItemRepository.Update(foodItemToUpdate, foodItemHasChanged, tagsChanged);
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

    public async Task Delete(int id)
    {
        await _FoodItemRepository.Delete(id);
    }
}