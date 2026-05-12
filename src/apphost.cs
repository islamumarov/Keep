#:sdk Aspire.AppHost.Sdk@13.3.0
#:package Aspire.Hosting.PostgreSQL@13.3.0
#:property NoWarn=ASPIRECSHARPAPPS001

var builder = DistributedApplication.CreateBuilder(args);

// Shared JWT config — issuer/audience are non-secret, secret key stored in user-secrets via parameter
var jwtSecret = builder.AddParameter("jwt-secret", secret: true);
const string jwtIssuer = "https://yourdomain.com";
const string jwtAudience = "todo-app";

// Postgres — persistent container, single instance hosts both databases
var postgres = builder.AddPostgres("postgres").WithImageTag("latest").WithContainerName("postgres_keep").WithHostPort(5432);
    // .WithLifetime(ContainerLifetime.Persistent)
    // .WithDataVolume("keep-pgdata");

var authDb = postgres.AddDatabase("keep-auth", "keep_auth");
var todoDb = postgres.AddDatabase("keep-app", "keep_app");

var migrations = builder.AddCSharpApp("migrations", "./Keep.MigrationService")
    .WithReference(authDb)
    .WithReference(todoDb)
    .WaitFor(authDb)
    .WaitFor(todoDb)
    .WithEnvironment("ConnectionStrings__AuthDb", authDb.Resource.ConnectionStringExpression)
    .WithEnvironment("ConnectionStrings__TodoDb", todoDb.Resource.ConnectionStringExpression);

var auth = builder.AddCSharpApp("auth", "./auth-service/AuthService")
    .WithReference(authDb)
    .WaitFor(migrations)
    .WithEnvironment("ConnectionStrings__DefaultConnection", authDb.Resource.ConnectionStringExpression)
    .WithEnvironment("Jwt__Issuer", jwtIssuer)
    .WithEnvironment("Jwt__Audience", jwtAudience)
    .WithEnvironment("Jwt__SecretKey", jwtSecret)
    .WithEnvironment("Jwt__AccessTokenLifetimeMinutes", "60")
    .WithEnvironment("Jwt__RefreshTokenLifetimeDays", "7");

var todo = builder.AddCSharpApp("todo", "./todo-service/TodoService")
    .WithReference(todoDb)
    .WaitFor(migrations)
    .WithEnvironment("ConnectionStrings__TodoDb", todoDb.Resource.ConnectionStringExpression)
    .WithEnvironment("Jwt__Issuer", jwtIssuer)
    .WithEnvironment("Jwt__Audience", jwtAudience)
    .WithEnvironment("Jwt__SecretKey", jwtSecret);

var gateway = builder.AddCSharpApp("gateway", "./gateway")
    .WithReference(auth)
    .WithReference(todo)
    .WaitFor(auth)
    .WaitFor(todo)
    .WithEnvironment("Jwt__Issuer", jwtIssuer)
    .WithEnvironment("Jwt__Audience", jwtAudience)
    .WithEnvironment("Jwt__SecretKey", jwtSecret)
    .WithEnvironment("ReverseProxy__Clusters__auth-cluster__Destinations__auth1__Address", auth.GetEndpoint("https"))
    .WithEnvironment("ReverseProxy__Clusters__todo-cluster__Destinations__todo1__Address", todo.GetEndpoint("https"))
    .WithExternalHttpEndpoints();

builder.Build().Run();
