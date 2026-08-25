using StorePos.Application;
using StorePos.Api.ErrorHandling;
using StorePos.Persistence;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("StorePos")
    ?? throw new InvalidOperationException("Connection string 'StorePos' was not found.");

builder.Services.AddApplication();
builder.Services.AddPersistence(connectionString);
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();
app.MapControllers();

app.Run();
