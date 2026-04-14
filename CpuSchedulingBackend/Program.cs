using CpuSchedulingBackend.Interfaces;
using CpuSchedulingBackend.Middleware;
using CpuSchedulingBackend.Services;

var builder = WebApplication.CreateBuilder(args);
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";

// DI
builder.Services.AddSingleton<ISchedulerService, SchedulerService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
  
var app = builder.Build();

// Middleware
app.UseMiddleware<ExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

// app.UseHttpsRedirection(); // optional

app.UseCors("AllowAll");

app.UseAuthorization();
app.MapControllers();

app.Run($"http://0.0.0.0:{port}");