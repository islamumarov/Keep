using System.Text;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using TodoService.Api.Endpoints;
using TodoService.Application.Behaviors;
using TodoService.Application.Interfaces;
using TodoService.Infrastructure.Persistence;
using TodoService.Infrastructure.Persistence.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TodoDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("TodoDb")
        ?? throw new InvalidOperationException("Connection string 'TodoDb' not found.");

    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        // Optional: recommended settings for production
        npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(30), errorCodesToAdd: null);
        npgsqlOptions.MigrationsAssembly(typeof(TodoDbContext).Assembly.FullName); // if migrations in different assembly
    });

    // Optional: log SQL queries in dev (remove or conditional in prod)
#if DEBUG
    options.EnableSensitiveDataLogging();
    options.EnableDetailedErrors();
#endif
});

builder.Services.AddAuthentication(options =>    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!))
        };

        // Optional: if you want detailed failure logging during dev
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                {
                    context.Response.Headers.Add("Token-Expired", "true");
                }
                return Task.CompletedTask;
            }
        };

        // For dev: allow http metadata if testing without https
        options.RequireHttpsMetadata = builder.Environment.IsDevelopment() ? false : true;
    });

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Auth Service", Version = "v1" });
    
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter JWT with Bearer into field",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(document => new() { [new OpenApiSecuritySchemeReference("Bearer", document)] = [] });

});

builder.Services.AddScoped<ITodoRepository, TodoRepository>();
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(TodoEndpoints).Assembly); // or your Application assembly

    // Register pipeline behaviors – order matters!
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
    // cfg.AddOpenBehavior(typeof(ValidationBehavior<,>)); // add later
    // cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
});
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile(nameof(MappingProfile).GetType());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.MapTodoEndpoints();

app.MapGet("/api/todos/health", () => Results.Ok(new { status = "Todo service healthy" }));



app.Run();
