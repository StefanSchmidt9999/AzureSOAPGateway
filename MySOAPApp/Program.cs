//var builder = WebApplication.CreateBuilder(args);

//// Add services to the container.

//builder.Services.AddControllers();
//// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi();

//var app = builder.Build();

//// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.MapOpenApi();
//}

//app.UseAuthorization();

//app.UseStaticFiles();

//app.MapControllers();

//app.Run();
var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddControllers();
builder.Services.AddRazorPages();

builder.Services.AddOpenApi();

var app = builder.Build();

// OpenAPI nur im Entwicklungsmodus
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseStaticFiles();

app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.Run();