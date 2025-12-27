using MedcodeETLProcess.Configurations;
using MedcodeETLProcess.Services;
using Medcode.Presentation.Hubs;
using Medcode.Presentation.Notifications;
using MedcodeETLProcess.Contracts;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
//Log.Logger = new LoggerConfiguration()
//    .ReadFrom.Configuration(builder.Configuration)
//    .CreateLogger();

//builder.Host.UseSerilog(); // Use Serilog instead of default logging

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
            .WithOrigins("http://localhost:4200", "http://localhost", "http://localhost/ImportMedcodeClient",
            "https://universalmedicalrecord.com/ImportMedcodeClient")
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

if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseAuthorization();

app.UseCors("AllowAll");

app.MapControllers();
app.MapHub<ETLHub>("/hubs/etl");

//try
//{
    //Log.Information("Starting web application");
    app.Run();
//}
//catch (Exception ex)
//{
//    Log.Fatal(ex, "Application terminated unexpectedly");
//}
//finally
//{
//    Log.CloseAndFlush();
//}
