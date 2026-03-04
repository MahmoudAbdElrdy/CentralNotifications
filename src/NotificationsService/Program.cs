using BuildingBlocks.Notifications.DI;
using BuildingBlocks.Notifications.Realtime;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddCors(p => p.AddDefaultPolicy(policy =>
    policy.AllowAnyHeader().AllowAnyMethod().SetIsOriginAllowed(_ => true).AllowCredentials()
));

builder.Services.AddNotificationsBuildingBlockSqlServer(
    builder.Configuration.GetConnectionString("NotificationsDb")!,
    opt =>
    {
        opt.EnableSignalR = true;
        opt.EnableFcm = true;
        opt.EnableHms = true;
    }
);

var app = builder.Build();

app.UseCors();
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();
