using System.Text.Json.Serialization;
using WebApi.Helpers;
using WebApi.Repositories;
using WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

// add services to DI container
{
    var services = builder.Services;
    var env = builder.Environment;

    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen();

    var configuration = builder.Configuration;

    services.AddSingleton<DataContext>();
    services.AddCors(options =>
    {
        options.AddDefaultPolicy(builder =>
        {
            builder.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });

    services.AddControllers().AddJsonOptions(x =>
        {
            // serialize enums as strings in api responses (e.g. Role)
            x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

            // ignore omitted parameters on models to enable optional params (e.g. FoodItem update)
            x.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });

    // configure DI for application services
    services.AddScoped<IFoodItemRepository, FoodItemRepository>();
    services.AddScoped<IFoodItemService, FoodItemService>();

    //services.AddAuthentication().AddGoogle(googleOptions =>
    //{
    //    googleOptions.ClientId = configuration["Authentication:ClientId"];
    //    googleOptions.ClientSecret = configuration["Authentication:ClientSecret"];
    //});
}

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// ensure database and tables exist
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<DataContext>();
    await context.Init();
}

// configure HTTP request pipeline
{
    // global cors policy
    app.UseCors();

    // global error handler
    app.UseMiddleware<ErrorHandlerMiddleware>();

    app.MapControllers();

    app.MapSwagger();

    app.Run();
}