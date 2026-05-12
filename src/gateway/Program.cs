using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// ---- Add Yarp
builder
    .Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// AUTH
builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = null; // we're self-contained → manual validation
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"], // same as auth-service
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!)
            )
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();

// Allow anonymous access for auth endpoints, but enforce auth for protected paths
app.Use(
    async (context, next) =>
    {
        var path = context.Request.Path;

        // Let auth service endpoints be reachable without a JWT
        if (path.StartsWithSegments("/api/auth"))
        {
            await next();
            return;
        }

        // Protect todo endpoints at the gateway level
        if (path.StartsWithSegments("/api/todos"))
        {
            if (context.User?.Identity?.IsAuthenticated != true)
            {
                await context.ChallengeAsync();
                return;
            }
        }

        await next();
    }
);

app.UseAuthorization();

app.MapReverseProxy(proxyPipeline =>
    {
        proxyPipeline.Use(
            async (context, next) =>
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                // Forward user ID as header (common pattern)
                var userId = context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    context.Request.Headers["X-User-Id"] = userId;
                }

                // Optional: forward full Authorization header
                // context.Request.Headers["Authorization"] remains as-is by default in YARP
            }

            await next(context);
            }
        );
});
app.MapDefaultEndpoints();

app.Run();
