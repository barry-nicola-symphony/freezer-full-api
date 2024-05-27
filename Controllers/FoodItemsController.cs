namespace WebApi.Controllers;

using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Models.FoodItems;
using Services;

[EnableCors]
[ApiController]
[Route("[controller]")]
public class FoodItemsController : ControllerBase
{
    private readonly IFoodItemService _foodItemService;

    public FoodItemsController(IFoodItemService FoodItemService)
    {
        _foodItemService = FoodItemService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var foodItems = await _foodItemService.GetAll();
        return Ok(foodItems);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var foodItems = await _foodItemService.GetById(id);
        return Ok(foodItems);
    }

    [HttpGet("[action]/{name}")]
    public async Task<IActionResult> FoodItem(string name)
    {
        var foodItems = await _foodItemService.GetByName(name);
        return Ok(foodItems);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateRequest model)
    {
        await _foodItemService.Create(model);
        return Ok();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateRequest model)
    {
        await _foodItemService.Update(id, model);
        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _foodItemService.Delete(id);
        return Ok();
    }
}