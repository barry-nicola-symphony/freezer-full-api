using WebApi.Entities;

namespace WebApi.Models.FoodItems;
public class UpdateRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? DateFrozen { get; set; }
    public int? Quantity { get; set; }
    public string? FreezerLocation { get; set; }
    public string? ItemLocation { get; set; }
    public string? Category { get; set; }
    public List<Tag>? Tags { get; set; }
    // helpers

    private string? replaceEmptyWithNull(string? value)
    {
        // replace empty string with null to make field optional
        return string.IsNullOrEmpty(value) ? null : value;
    }
}