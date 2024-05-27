namespace WebApi.Helpers;

using AutoMapper;
using WebApi.Entities;
using WebApi.Models.FoodItems;

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        // CreateRequest -> FoodItem
        CreateMap<CreateRequest, FoodItem>();

        // UpdateRequest -> FoodItem
        CreateMap<UpdateRequest, FoodItem>()
            .ForAllMembers(x => x.Condition(
                (src, dest, prop) =>
                {
                    // ignore both null & empty string properties
                    if (prop == null) return false;
                    //if (prop.GetType() == typeof(string) && string.IsNullOrEmpty((string)prop)) return true;

                    return true;
                }
            ));
    }
}