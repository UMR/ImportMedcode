using MedcodeETLProcess.Configurations;
using MedcodeETLProcess.Services;
using Medcode.Presentation.Hubs;
using Medcode.Presentation.Notifications;
using MedcodeETLProcess.Contracts;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithOrigins("*")
            .AllowCredentials();
    });
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register ETL services
ServiceConfigurations.ConfigureServices(builder.Services, builder.Configuration);
builder.Services.AddScoped<ETLService>();
builder.Services.AddSingleton<IProgressNotifier, SignalRProgressNotifier>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.UseCors("AllowAll");

app.MapControllers();
app.MapHub<ETLHub>("/hubs/etl");

app.Run();
