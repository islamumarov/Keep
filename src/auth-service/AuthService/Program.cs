using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthService.Data;
using AuthService.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.User.RequireUniqueEmail = true;
    }
).AddEntityFrameworkStores<AuthDbContext>()
.AddDefaultTokenProviders();


builder.Services.AddAuthentication(options =>
    {
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

var app = builder.Build();

// Development middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapDefaultEndpoints();

app.UseAuthentication();
app.UseAuthorization();
// ── Endpoints ───────────────────────────────────────────────
var auth = app.MapGroup("/api/auth");

    auth.MapPost("/register", async (HttpContext http,
            UserManager<ApplicationUser> userManager,
            [FromBody] RegisterRequest registerCommand,
            IConfiguration config) =>
        {
            if (registerCommand == null) return Results.BadRequest();

            var user = new ApplicationUser
            {
                UserName = registerCommand.Email,
                Email = registerCommand.Email,
            };

            var result = await userManager.CreateAsync(user, registerCommand.Password);
            return !result.Succeeded ? Results.BadRequest(new { errors = result.Errors }) : Results.Ok(new { message = "User created", userId = user.Id });

        })
        .WithName("Register");
    auth.MapPost("/login", async (HttpContext http,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            [FromBody] LoginRequest loginCommand,
            IConfiguration config) =>
        {
            if (loginCommand == null) return Results.BadRequest();

            var user = await userManager.FindByEmailAsync(loginCommand.Email);
            if (user == null) return Results.Unauthorized();

            var result = await signInManager.CheckPasswordSignInAsync(user, loginCommand.Password, lockoutOnFailure: false);
            if (!result.Succeeded) return Results.Unauthorized();

            // Generate JWT
            var token = GenerateJwtToken(user, config);
            return Results.Ok(new { access_token = token });
        })
        .WithName("Login");
    auth.MapGet("/me", async (ClaimsPrincipal user, UserManager<ApplicationUser> userManager) =>
    {
        var appUser = await userManager.GetUserAsync(user);
        if (appUser == null) return Results.Unauthorized();

        return Results.Ok(new
        {
            id = appUser.Id,
            email = appUser.Email
        });
    }).RequireAuthorization();


app.Run();


static string GenerateJwtToken(ApplicationUser user, IConfiguration config)
{
    var claims = new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
    };

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:SecretKey"]!));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: config["Jwt:Issuer"],
        audience: config["Jwt:Audience"],
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(config.GetValue<double>("Jwt:AccessTokenLifetimeMinutes")),
        signingCredentials: creds
    );
    
    return new JwtSecurityTokenHandler().WriteToken(token);
}