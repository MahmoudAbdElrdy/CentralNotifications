var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient("notifications");

var app = builder.Build();
app.MapControllers();
app.Run();
