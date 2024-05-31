namespace WebApi.Controllers;

using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Models.FoodItems;
using Services;

[EnableCors]
[ApiController]
[Route("[controller]")]
public class FoodItemsController(IFoodItemService foodItemService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAllFoodItems()
    {
        var foodItems = await foodItemService.GetAllFoodItems();
        return Ok(foodItems);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetFoodItemById(int id)
    {
        var foodItems = await foodItemService.GetFoodItemById(id);
        return Ok(foodItems);
    }

    [HttpGet("[action]/{name}")]
    public async Task<IActionResult> FoodItem(string name)
    {
        var foodItems = await foodItemService.GetFoodItemByName(name);
        return Ok(foodItems);
    }

    [HttpPost]
    public async Task<IActionResult> CreateFoodItem(CreateRequest model)
    {
        await foodItemService.CreateFoodItem(model);
        return Ok();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateFoodItem(int id, UpdateRequest model)
    {
        await foodItemService.UpdateFoodItemAndTags(id, model);
        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFoodItem(int id)
    {
        await foodItemService.DeleteFoodItem(id);
        return Ok();
    }

    [HttpGet("[action]")]
    public async Task<IActionResult> GetAllTags()
    {
        var tags = await foodItemService.GetAllTags();
        return Ok(tags);
    }

    [HttpGet("[action]/{id:int}")]
    public async Task<IActionResult> GetTagsForFoodItem(int id)
    {
        var foodItemId = Convert.ToInt32(id);
        var tags = await foodItemService.GetSelectedTagsForFoodItem(foodItemId);
        return Ok(tags);
    }


    [HttpPost("[action]/{tagName}")]
    public async Task<IActionResult> CreateTag(string tagName)
    {
        var result = await foodItemService.CreateTag(tagName);
        return Ok(result);
    }

    [HttpPut("[action]/{id:int}")]
    public async Task<IActionResult> UpdateTag(int id, string tagName)
    {
        await foodItemService.UpdateTag(id, tagName);
        return Ok();
    }

    [HttpDelete(template: "[action]/{id}")]
    public async Task<IActionResult> DeleteTag(int id)
    {
        await foodItemService.DeleteTag(id);
        return Ok();
    }
}