using AuthService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TodoService.Infrastructure.Persistence;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("AuthDb")));

builder.Services.AddDbContext<TodoDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TodoDb")));

var app = builder.Build();

using var scope = app.Services.CreateScope();

var authDb = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
await authDb.Database.MigrateAsync();
Console.WriteLine("Auth migrations applied.");

var todoDb = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
await todoDb.Database.MigrateAsync();
Console.WriteLine("Todo migrations applied.");
