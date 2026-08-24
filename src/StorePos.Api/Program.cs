using StorePos.Application;
using StorePos.Persistence;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("StorePos")
    ?? throw new InvalidOperationException("Connection string 'StorePos' was not found.");

builder.Services.AddApplication();
builder.Services.AddPersistence(connectionString);
builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();
