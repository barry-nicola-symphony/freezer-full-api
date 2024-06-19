using WebApi.Entities;

namespace WebApi.Models.FoodItems;

using System.ComponentModel.DataAnnotations;

public class CreateRequest
{
    [Required]
    public string? Name { get; set; }

    public string? Description { get; set; }
    public string? DateStored { get; set; }

    [Required]
    public decimal? Quantity { get; set; }

    [Required]
    public string? StorageLocation { get; set; }

    [Required]
    public string? ItemLocation { get; set; }
    public List<Tag>? Tags { get; set; }
}