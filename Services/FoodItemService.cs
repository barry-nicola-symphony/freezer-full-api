namespace WebApi.Services;

using AutoMapper;
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
    private readonly IMapper _mapper;

    public FoodItemService(
        IFoodItemRepository FoodItemRepository,
        IMapper mapper)
    {
        _FoodItemRepository = FoodItemRepository;
        _mapper = mapper;
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
        var foodItem = await _FoodItemRepository.GetById(id);

        if (foodItem == null)
            throw new KeyNotFoundException("FoodItem not found");

        // validate

        // copy model props to FoodItem
        _mapper.Map(model, foodItem);

        // save FoodItem
        //await _FoodItemRepository.Update(foodItem);
    }

    public async Task Delete(int id)
    {
        await _FoodItemRepository.Delete(id);
    }
}