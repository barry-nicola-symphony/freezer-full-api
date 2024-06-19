namespace WebApi.Entities;
public class FoodItem
{
    public int FoodItemId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? DateStored { get; set; }
    public int Quantity { get; set; }
    public string? StorageLocation { get; set; }
    public string? ItemLocation { get; set; }
    public string? Category { get; set; }
    public List<Tag>? Tags { get; set; }
}